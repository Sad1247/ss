using System; using BellaNotte.Core;
class T {
  static int fails = 0;
  static void Check(string nom, bool ok){ Console.WriteLine((ok?"OK   ":"ECHEC")+" "+nom); if(!ok) fails++; }
  static void Main(){
    // 1. pizza parfaite = prix + pourboire
    var g = new PizzeriaGame(42);
    ServiceResult dernier = null;
    g.PizzaServie += r => dernier = r;
    g.Demarrer();
    var o = g.CommandeActive;
    g.Basculer(o.Recette.Base);
    foreach(var t in o.Recette.Garnitures) g.Basculer(t);
    g.Enfourner();
    for(int i=0;i<100 && g.PizzaEnCours.Cuisson < 1.0f;i++) g.PizzaEnCours.Cuire(0.05f);
    g.SortirDuFour();
    g.Servir();
    Check("pizza exacte + cuisson parfaite => ratio 1", dernier != null && dernier.EstParfaite);
    Check("paiement = prix + 50% pourboire",
          dernier.Paiement == o.Recette.Prix && dernier.Pourboire == (int)Math.Round(o.Recette.Prix*0.5));

    // 2. garniture manquante => paiement reduit, pas nul
    var g2 = new PizzeriaGame(7); ServiceResult d2=null; g2.PizzaServie += r=>d2=r; g2.Demarrer();
    var o2 = g2.CommandeActive;
    g2.Basculer(o2.Recette.Base);
    for(int i=0;i<o2.Recette.Garnitures.Count-1;i++) g2.Basculer(o2.Recette.Garnitures[i]);
    g2.Enfourner();
    for(int i=0;i<100 && g2.PizzaEnCours.Cuisson<1.0f;i++) g2.PizzaEnCours.Cuire(0.05f);
    g2.SortirDuFour(); g2.Servir();
    Check("garniture manquante => paiement partiel", d2.Paiement>0 && d2.Paiement<o2.Recette.Prix && d2.Manquants.Count==1);

    // 3. cramee => refusee et client perdu
    var g3 = new PizzeriaGame(3); ServiceResult d3=null; g3.PizzaServie += r=>d3=r; g3.Demarrer();
    var o3 = g3.CommandeActive;
    g3.Basculer(o3.Recette.Base);
    foreach(var t in o3.Recette.Garnitures) g3.Basculer(t);
    g3.Enfourner();
    for(int i=0;i<300 && g3.PizzaEnCours.AuFour;i++) g3.PizzaEnCours.Cuire(0.05f);
    Check("sortie auto du four a 1.6", !g3.PizzaEnCours.AuFour && g3.PizzaEnCours.Sortie);
    g3.Servir();
    Check("cramee => refusee", d3.EstRefusee && g3.ClientsPerdus==1);

    // 4. garde-fous de composition
    var p = new Pizza(); string refus;
    Check("garniture sans base refusee", !p.Basculer(IngredientId.Mozzarella, out refus) && refus!=null);
    p.Basculer(IngredientId.Tomate, out refus);
    foreach(var t in new[]{IngredientId.Mozzarella,IngredientId.Jambon,IngredientId.Olive,IngredientId.Ananas,IngredientId.Piment})
      p.Basculer(t, out refus);
    Check("6e garniture refusee", !p.Basculer(IngredientId.Basilic, out refus) && p.Garnitures.Count==5);

    // 5. patience : le client part
    var g5 = new PizzeriaGame(11); bool parti=false; g5.Demarrer(); g5.CommandePartie += _=>parti=true;
    for(int i=0;i<2000 && !parti;i++) g5.Tick(0.1f);
    Check("client impatient finit par partir", parti && g5.ClientsPerdus>=1);

    // 6. fin de partie a 3 clients perdus
    var g6 = new PizzeriaGame(5); bool fin=false; g6.Demarrer(); g6.PartieTerminee += ()=>fin=true;
    for(int i=0;i<6000 && !fin;i++) g6.Tick(0.1f);
    Check("partie terminee a 3 perdus", fin && g6.ClientsPerdus>=3 && !g6.EnCours);

    Console.WriteLine(fails==0 ? "\nTOUS LES TESTS PASSENT" : "\n"+fails+" ECHEC(S)");
    Environment.Exit(fails==0?0:1);
  }
}
