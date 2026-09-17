// Plan Bureaux — application de bureau pour Windows.
//
// L'exécutable embarque la page du plan, la sert sur 127.0.0.1 et ouvre le
// navigateur par défaut. Les attributions sont enregistrées dans
// plan-bureaux.json, à côté de l'exécutable (ou dans %APPDATA%\PlanBureaux
// quand le dossier n'est pas accessible en écriture).
package main

import (
	"bufio"
	"embed"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net"
	"net/http"
	"os"
	"os/exec"
	"os/signal"
	"path/filepath"
	"runtime"
	"sync"
	"syscall"
	"time"
)

//go:embed plan-bureaux.html
var assets embed.FS

const (
	appName     = "Plan Bureaux"
	dataName    = "plan-bureaux.json"
	firstPort   = 7828
	portTries   = 20
	maxBodySize = 1 << 20 // 1 Mo : très large pour 28 postes
)

var (
	mu       sync.Mutex
	dataPath string
)

func main() {
	page, err := assets.ReadFile("plan-bureaux.html")
	if err != nil {
		fatal("Page introuvable dans l'exécutable : %v", err)
	}

	dataPath = resolveDataPath()

	ln, url, err := listen()
	if err != nil {
		// Le port est peut-être déjà pris par une instance en cours : on ouvre
		// simplement le navigateur dessus plutôt que d'échouer.
		existing := fmt.Sprintf("http://127.0.0.1:%d/", firstPort)
		if reachable(existing) {
			fmt.Printf("%s est déjà ouvert. J'affiche la fenêtre existante.\n", appName)
			openBrowser(existing)
			return
		}
		fatal("Impossible d'ouvrir un port local : %v", err)
	}

	mux := http.NewServeMux()
	mux.HandleFunc("/api/data", handleData)
	mux.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path != "/" {
			http.NotFound(w, r)
			return
		}
		w.Header().Set("Content-Type", "text/html; charset=utf-8")
		w.Header().Set("Cache-Control", "no-store")
		_, _ = w.Write(page)
	})

	srv := &http.Server{
		Handler:           localOnly(mux),
		ReadHeaderTimeout: 5 * time.Second,
	}

	banner(url)

	go func() {
		if err := srv.Serve(ln); err != nil && !errors.Is(err, http.ErrServerClosed) {
			fatal("Le serveur local s'est arrêté : %v", err)
		}
	}()

	time.Sleep(250 * time.Millisecond)
	openBrowser(url)

	stop := make(chan os.Signal, 1)
	signal.Notify(stop, os.Interrupt, syscall.SIGTERM)
	<-stop
	fmt.Println("\nFermeture. Vos attributions sont enregistrées.")
}

func banner(url string) {
	fmt.Printf("\n  %s\n", appName)
	fmt.Printf("  %s\n\n", "──────────────────────────────")
	fmt.Printf("  Le plan est ouvert dans votre navigateur :\n    %s\n\n", url)
	fmt.Printf("  Attributions enregistrées dans :\n    %s\n\n", dataPath)
	fmt.Printf("  Laissez cette fenêtre ouverte tant que vous utilisez le plan.\n")
	fmt.Printf("  Pour quitter : fermez cette fenêtre ou appuyez sur Ctrl+C.\n\n")
}

// listen ouvre le premier port libre à partir de firstPort, sur la boucle
// locale uniquement : rien n'est exposé sur le réseau de l'entreprise.
func listen() (net.Listener, string, error) {
	var last error
	for p := firstPort; p < firstPort+portTries; p++ {
		ln, err := net.Listen("tcp", fmt.Sprintf("127.0.0.1:%d", p))
		if err == nil {
			return ln, fmt.Sprintf("http://127.0.0.1:%d/", p), nil
		}
		last = err
	}
	return nil, "", last
}

func reachable(url string) bool {
	c := &http.Client{Timeout: 700 * time.Millisecond}
	resp, err := c.Get(url)
	if err != nil {
		return false
	}
	defer resp.Body.Close()
	return resp.StatusCode == http.StatusOK
}

// localOnly refuse tout ce qui ne vient pas de la machine elle-même.
func localOnly(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		host, _, err := net.SplitHostPort(r.RemoteAddr)
		if err != nil || !net.ParseIP(host).IsLoopback() {
			http.Error(w, "Accès local uniquement", http.StatusForbidden)
			return
		}
		next.ServeHTTP(w, r)
	})
}

func handleData(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Cache-Control", "no-store")

	switch r.Method {
	case http.MethodGet:
		mu.Lock()
		raw, err := os.ReadFile(dataPath)
		mu.Unlock()
		if err != nil || len(raw) == 0 {
			raw = []byte("{}")
		}
		if !json.Valid(raw) {
			raw = []byte("{}")
		}
		w.Header().Set("Content-Type", "application/json; charset=utf-8")
		_, _ = w.Write(raw)

	case http.MethodPost:
		body, err := io.ReadAll(io.LimitReader(r.Body, maxBodySize))
		if err != nil {
			http.Error(w, "Lecture impossible", http.StatusBadRequest)
			return
		}
		var probe map[string]any
		if err := json.Unmarshal(body, &probe); err != nil {
			http.Error(w, "Données invalides", http.StatusBadRequest)
			return
		}
		pretty, err := json.MarshalIndent(probe, "", " ")
		if err != nil {
			pretty = body
		}
		mu.Lock()
		err = writeAtomic(dataPath, pretty)
		mu.Unlock()
		if err != nil {
			fmt.Printf("Enregistrement impossible : %v\n", err)
			http.Error(w, "Enregistrement impossible", http.StatusInternalServerError)
			return
		}
		w.WriteHeader(http.StatusNoContent)

	default:
		w.Header().Set("Allow", "GET, POST")
		http.Error(w, "Méthode non autorisée", http.StatusMethodNotAllowed)
	}
}

// writeAtomic garde une copie de la version précédente puis remplace le
// fichier d'un seul coup, pour qu'une coupure ne laisse jamais de JSON tronqué.
func writeAtomic(path string, data []byte) error {
	if old, err := os.ReadFile(path); err == nil && len(old) > 0 {
		_ = os.WriteFile(path+".bak", old, 0o600)
	}
	tmp := path + ".tmp"
	f, err := os.OpenFile(tmp, os.O_CREATE|os.O_TRUNC|os.O_WRONLY, 0o600)
	if err != nil {
		return err
	}
	w := bufio.NewWriter(f)
	if _, err := w.Write(data); err != nil {
		f.Close()
		return err
	}
	if err := w.Flush(); err != nil {
		f.Close()
		return err
	}
	if err := f.Sync(); err != nil {
		f.Close()
		return err
	}
	if err := f.Close(); err != nil {
		return err
	}
	return os.Rename(tmp, path)
}

// resolveDataPath préfère le dossier de l'exécutable (fichier visible, facile à
// sauvegarder) et bascule sur %APPDATA% si ce dossier est en lecture seule.
func resolveDataPath() string {
	if exe, err := os.Executable(); err == nil {
		dir := filepath.Dir(exe)
		if writable(dir) {
			return filepath.Join(dir, dataName)
		}
	}
	base, err := os.UserConfigDir()
	if err != nil {
		base = os.TempDir()
	}
	dir := filepath.Join(base, "PlanBureaux")
	_ = os.MkdirAll(dir, 0o700)
	return filepath.Join(dir, dataName)
}

func writable(dir string) bool {
	f, err := os.CreateTemp(dir, ".planbureaux-*")
	if err != nil {
		return false
	}
	name := f.Name()
	f.Close()
	_ = os.Remove(name)
	return true
}

func openBrowser(url string) {
	var cmd *exec.Cmd
	switch runtime.GOOS {
	case "windows":
		cmd = exec.Command("rundll32", "url.dll,FileProtocolHandler", url)
	case "darwin":
		cmd = exec.Command("open", url)
	default:
		cmd = exec.Command("xdg-open", url)
	}
	if err := cmd.Start(); err != nil {
		fmt.Printf("Ouvrez cette adresse dans votre navigateur : %s\n", url)
		return
	}
	go func() { _ = cmd.Wait() }()
}

func fatal(format string, args ...any) {
	fmt.Printf("\n"+format+"\n\nAppuyez sur Entrée pour fermer.\n", args...)
	_, _ = bufio.NewReader(os.Stdin).ReadString('\n')
	os.Exit(1)
}
