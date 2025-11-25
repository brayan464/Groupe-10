using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System;
using System.Data.SQLite;
using System.Xml.Linq;

namespace GestionStock
{
    // ===========================
    // CLASSE DE BASE
    // ===========================
    public abstract class Utilisateur
    {
        public static string dbPath = @"C:\Users\ngoch\source\repos\ConsoleStock\gestionStock.db";

        public int Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }

        public virtual void SeConnecter() => Console.WriteLine($"{Nom} {Prenom} se connecte...");
        public virtual void SeDeconnecter() => Console.WriteLine($"{Nom} {Prenom} se déconnecte...");
    }

    
    public abstract class Admin : Utilisateur { }

    public class AdminProduits : Admin
    {
        // Ajouter un produit
        public void AjouterProduit()
        {
            Console.WriteLine("=== AJOUT D'UN PRODUIT ===");
            Console.Write("Nom : "); string nom = Console.ReadLine();
            Console.Write("Description : "); string desc = Console.ReadLine();
            Console.Write("Prix unitaire : "); double prix = double.Parse(Console.ReadLine());
            Console.Write("Quantité : "); int quant = int.Parse(Console.ReadLine());
            Console.Write("Type produit : "); string type = Console.ReadLine();
            Console.Write("Date expiration (YYYY-MM-DD) : "); string dateExp = Console.ReadLine();
            Console.Write("Marque : "); string marque = Console.ReadLine();
            Console.Write("Modèle : "); string modele = Console.ReadLine();
            Console.Write("Garantie (mois) : "); int garantie = int.Parse(Console.ReadLine());

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query = @"INSERT INTO Produit 
                    (Nom, Description, PrixUnitaire, QuantiteStock, TypeProduit, DateExpiration, Marque, Modele, GarantieMois)
                    VALUES (@nom, @desc, @prix, @quant, @type, @dateExp, @marque, @modele, @garantie)";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@nom", nom);
                    cmd.Parameters.AddWithValue("@desc", desc);
                    cmd.Parameters.AddWithValue("@prix", prix);
                    cmd.Parameters.AddWithValue("@quant", quant);
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@dateExp", dateExp);
                    cmd.Parameters.AddWithValue("@marque", marque);
                    cmd.Parameters.AddWithValue("@modele", modele);
                    cmd.Parameters.AddWithValue("@garantie", garantie);

                    int rows = cmd.ExecuteNonQuery();
                    Console.WriteLine(rows > 0 ? "Produit ajouté avec succès !" : "Erreur lors de l'ajout.");
                }
            }
        }

        // Modifier un produit
        public void ModifierProduit()
        {
            Console.WriteLine("=== MODIFICATION D'UN PRODUIT ===");
            Console.Write("ID du produit : "); int id = int.Parse(Console.ReadLine());
            Console.Write("Nouveau nom : "); string nom = Console.ReadLine();
            Console.Write("Nouvelle description : "); string desc = Console.ReadLine();
            Console.Write("Nouveau prix : "); double prix = double.Parse(Console.ReadLine());
            Console.Write("Nouvelle quantité : "); int quant = int.Parse(Console.ReadLine());

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query = @"UPDATE Produit
                                 SET Nom=@nom, Description=@desc, PrixUnitaire=@prix, QuantiteStock=@quant
                                 WHERE ProduitId=@id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@nom", nom);
                    cmd.Parameters.AddWithValue("@desc", desc);
                    cmd.Parameters.AddWithValue("@prix", prix);
                    cmd.Parameters.AddWithValue("@quant", quant);
                    cmd.Parameters.AddWithValue("@id", id);

                    int rows = cmd.ExecuteNonQuery();
                    Console.WriteLine(rows > 0 ? "Produit modifié avec succès !" : "ID introuvable ou erreur.");
                }
            }
        }

        // Supprimer un produit
        public void SupprimerProduit()
        {
            Console.WriteLine("=== SUPPRESSION D'UN PRODUIT ===");
            Console.Write("ID du produit : "); int id = int.Parse(Console.ReadLine());

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query = "DELETE FROM Produit WHERE ProduitId=@id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int rows = cmd.ExecuteNonQuery();
                    Console.WriteLine(rows > 0 ? "Produit supprimé avec succès !" : "ID introuvable ou erreur.");
                }
            }
        }
    }

    // AdminClients
    public class AdminClients : Admin
    {
        // ===========================
        // AJOUTER UN CLIENT
        // ===========================
        public void AjouterClient()
        {
            Console.WriteLine("=== AJOUT D'UN CLIENT ===");
            Console.Write("Nom : "); string nom = Console.ReadLine();
            Console.Write("Prénom : "); string prenom = Console.ReadLine();
            Console.Write("Email : "); string email = Console.ReadLine();
            Console.Write("Téléphone : "); string tel = Console.ReadLine();
            Console.Write("Nom d'utilisateur : "); string username = Console.ReadLine();
            Console.Write("Mot de passe : "); string password = Console.ReadLine();
            Console.Write("Rôle : "); string role = Console.ReadLine(); // Caissier, GestionnaireStock, etc.

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query = @"INSERT INTO Utilisateur 
                (Nom, Prenom, Email, Telephone, Username, PasswordHash, Role)
                VALUES (@nom, @prenom, @email, @tel, @username, @pass, @role)";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@nom", nom);
                    cmd.Parameters.AddWithValue("@prenom", prenom);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@tel", tel);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@pass", password);
                    cmd.Parameters.AddWithValue("@role", role);

                    int rows = cmd.ExecuteNonQuery();
                    Console.WriteLine(rows > 0 ? "Client ajouté avec succès !" : "Erreur lors de l'ajout.");
                }
            }
        }

        // ===========================
        // MODIFIER UN CLIENT
        // ===========================
        public void ModifierClient()
        {
            Console.WriteLine("=== MODIFICATION D'UN CLIENT ===");
            Console.Write("ID du client : "); int id = int.Parse(Console.ReadLine());
            Console.Write("Nouveau nom : "); string nom = Console.ReadLine();
            Console.Write("Nouveau prénom : "); string prenom = Console.ReadLine();
            Console.Write("Nouvel email : "); string email = Console.ReadLine();
            Console.Write("Nouveau téléphone : "); string tel = Console.ReadLine();
            Console.Write("Nouveau nom d'utilisateur : "); string username = Console.ReadLine();
            Console.Write("Nouveau mot de passe : "); string password = Console.ReadLine();
            Console.Write("Nouveau rôle : "); string role = Console.ReadLine();

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query = @"UPDATE Utilisateur
                             SET Nom=@nom, Prenom=@prenom, Email=@email, Telephone=@tel, 
                                 Username=@username, PasswordHash=@pass, Role=@role
                             WHERE UserId=@id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@nom", nom);
                    cmd.Parameters.AddWithValue("@prenom", prenom);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@tel", tel);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@pass", password);
                    cmd.Parameters.AddWithValue("@role", role);
                    cmd.Parameters.AddWithValue("@id", id);

                    int rows = cmd.ExecuteNonQuery();
                    Console.WriteLine(rows > 0 ? "Client modifié avec succès !" : "ID introuvable ou erreur.");
                }
            }
        }

        // ===========================
        // SUPPRIMER UN CLIENT
        // ===========================
        public void SupprimerClient()
        {
            Console.WriteLine("=== SUPPRESSION D'UN CLIENT ===");
            Console.Write("ID du client : "); int id = int.Parse(Console.ReadLine());

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query = "DELETE FROM Utilisateur WHERE UserId=@id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int rows = cmd.ExecuteNonQuery();
                    Console.WriteLine(rows > 0 ? "Client supprimé avec succès !" : "ID introuvable ou erreur.");
                }
            }
        }

        // ===========================
        // AFFICHER TOUS LES CLIENTS
        // ===========================
        public void AfficherUtilisateurs()
        {
            Console.WriteLine("=== LISTE DES UTILISATEURS ===");
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query = "SELECT UserId, Nom, Prenom, Email, Telephone, Username, Role FROM Utilisateur";
                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"ID: {reader["UserId"]} | Nom: {reader["Nom"]} {reader["Prenom"]} | Email: {reader["Email"]} | Tel: {reader["Telephone"]} | Username: {reader["Username"]} | Role: {reader["Role"]}");
                    }
                }
            }
        }
    }


    // AdminTransactions
    public class AdminTransactions : Admin
    {
        // ===========================
        // LISTER TOUTES LES TRANSACTIONS
        // ===========================
        public void ListerTransactions()
        {
            Console.WriteLine("=== LISTE DES TRANSACTIONS ===");

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query = @"
                SELECT t.TransactionId, t.FactureId, t.Date, t.Montant, t.PersonnelId, t.TypeTransaction,
                       u.Nom || ' ' || u.Prenom AS PersonnelNom
                FROM Transactions t
                LEFT JOIN Utilisateur u ON t.PersonnelId = u.UserId
                ORDER BY t.Date ASC";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine(
                            $"ID: {reader["TransactionId"]} | Facture: {reader["FactureId"]} | Date: {reader["Date"]} | " +
                            $"Montant: {reader["Montant"]} | Personnel: {reader["PersonnelNom"]} | Type: {reader["TypeTransaction"]}"
                        );
                    }
                }
            }
        }
    }


    // AdminSysteme
    public class AdminSysteme : Admin
    {
        // ===========================
        // SAUVEGARDER LA BASE DE DONNÉES
        // ===========================
        public void SauvegarderBD()
        {
            try
            {
                Console.WriteLine("=== SAUVEGARDE DE LA BASE ===");

                string sourceFile = dbPath; // Chemin de la base SQLite
                string backupFile = dbPath.Replace(".db", $"_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

                System.IO.File.Copy(sourceFile, backupFile);
                Console.WriteLine($"Sauvegarde effectuée avec succès : {backupFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la sauvegarde : {ex.Message}");
            }
        }
    }


    // ===========================
    // Personnel
    // ===========================
    public abstract class Personnel : Utilisateur { }

    public class Caissier : Personnel
    {
        // ===========================
        // CREER UNE FACTURE
        // ===========================
        public void CreerFacture()
        {
            Console.WriteLine("=== CREATION D'UNE FACTURE ===");

            Console.Write("ID du client : ");
            int clientId = int.Parse(Console.ReadLine());

            Console.Write("Montant total : ");
            double total = double.Parse(Console.ReadLine());

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query = @"INSERT INTO Facture (ClientId, PersonnelId, DateFacture, Total)
                             VALUES (@clientId, @personnelId, @dateFacture, @total)";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@clientId", clientId);
                    cmd.Parameters.AddWithValue("@personnelId", this.Id);
                    cmd.Parameters.AddWithValue("@dateFacture", DateTime.Now.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@total", total);

                    int rows = cmd.ExecuteNonQuery();
                    Console.WriteLine(rows > 0 ? "Facture créée avec succès !" : "Erreur lors de la création.");
                }
            }
        }

        // ===========================
        // ENCAISSER UNE FACTURE
        // ===========================
        public void Encaisser()
        {
            Console.WriteLine("=== ENCAISSEMENT D'UNE FACTURE ===");
            Console.Write("ID de la facture : ");
            int factureId = int.Parse(Console.ReadLine());

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query = @"UPDATE Facture
                             SET DateFacture = @dateFacture
                             WHERE FactureId = @id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@dateFacture", DateTime.Now.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@id", factureId);

                    int rows = cmd.ExecuteNonQuery();
                    Console.WriteLine(rows > 0 ? "Facture encaissée avec succès !" : "ID introuvable ou erreur.");
                }
            }
        }
    }


    public class GestionnaireStock : Personnel
    {
        // ===========================
        // METTRE A JOUR LE STOCK D'UN PRODUIT
        // ===========================
        public void MettreAJourStock()
        {
            Console.WriteLine("=== MISE A JOUR DU STOCK ===");
            Console.Write("ID du produit : ");
            int produitId = int.Parse(Console.ReadLine());
            Console.Write("Nouvelle quantité en stock : ");
            int quantite = int.Parse(Console.ReadLine());

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query = @"UPDATE Produit 
                             SET QuantiteStock=@quant
                             WHERE ProduitId=@id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@quant", quantite);
                    cmd.Parameters.AddWithValue("@id", produitId);

                    int rows = cmd.ExecuteNonQuery();
                    Console.WriteLine(rows > 0 ? "Stock mis à jour avec succès !" : "ID introuvable ou erreur.");
                }
            }
        }

        // ===========================
        // RECEPTIONNER UN PRODUIT
        // ===========================
        public void ReceptionnerProduit()
        {
            Console.WriteLine("=== RECEPTION D'UN PRODUIT ===");
            Console.Write("Nom du produit : ");
            string nom = Console.ReadLine();
            Console.Write("Quantité reçue : ");
            int quantite = int.Parse(Console.ReadLine());

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                // Vérifie si le produit existe déjà
                string checkQuery = @"SELECT ProduitId, QuantiteStock FROM Produit WHERE Nom=@nom LIMIT 1";
                using (var cmdCheck = new SQLiteCommand(checkQuery, conn))
                {
                    cmdCheck.Parameters.AddWithValue("@nom", nom);
                    using (var reader = cmdCheck.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Produit existe, on ajoute la quantité
                            int id = Convert.ToInt32(reader["ProduitId"]);
                            int currentQuant = Convert.ToInt32(reader["QuantiteStock"]);
                            string updateQuery = @"UPDATE Produit SET QuantiteStock=@newQuant WHERE ProduitId=@id";
                            using (var cmdUpdate = new SQLiteCommand(updateQuery, conn))
                            {
                                cmdUpdate.Parameters.AddWithValue("@newQuant", currentQuant + quantite);
                                cmdUpdate.Parameters.AddWithValue("@id", id);
                                cmdUpdate.ExecuteNonQuery();
                            }
                            Console.WriteLine("Produit réceptionné et stock mis à jour !");
                        }
                        else
                        {
                            // Produit inexistant, on l'ajoute
                            string insertQuery = @"INSERT INTO Produit (Nom, QuantiteStock) VALUES (@nom, @quant)";
                            using (var cmdInsert = new SQLiteCommand(insertQuery, conn))
                            {
                                cmdInsert.Parameters.AddWithValue("@nom", nom);
                                cmdInsert.Parameters.AddWithValue("@quant", quantite);
                                cmdInsert.ExecuteNonQuery();
                            }
                            Console.WriteLine("Produit ajouté et stock mis à jour !");
                        }
                    }
                }
            }
        }
    }


    public class ResponsableVentes : Personnel
    {
        public void SuperviserVentes()
        {
            Console.WriteLine("=== SUPERVISION DES VENTES ===");

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string query = @"SELECT f.FactureId, c.Nom || ' ' || c.Prenom AS Client, f.Total, f.DateFacture
                             FROM Facture f
                             JOIN Utilisateur c ON f.ClientId = c.UserId
                             ORDER BY f.DateFacture DESC";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        Console.WriteLine("Aucune vente trouvée.");
                        return;
                    }

                    while (reader.Read())
                    {
                        int factureId = Convert.ToInt32(reader["FactureId"]);
                        string client = reader["Client"].ToString();
                        double total = Convert.ToDouble(reader["Total"]);
                        string date = reader["DateFacture"].ToString();

                        Console.WriteLine($"Facture #{factureId} | Client : {client} | Total : {total:C} | Date : {date}");
                    }
                }
            }
        }
    }


    public class AssistantBoutique : Personnel
    {
        public void AccueilClient()
        {
            Console.WriteLine("=== ACCUEIL CLIENT ===");
            Console.Write("Nom du client : ");
            string nom = Console.ReadLine();
            Console.Write("Prénom du client : ");
            string prenom = Console.ReadLine();

            Console.WriteLine($"Client {prenom} {nom} accueilli avec succès !");

        }
    }

public abstract class Consommateur : Utilisateur { }

    // ===========================
    // ClientSimple
    // ===========================
    public class ClientSimple : Consommateur
    {
        public void PasserCommande()
        {
            Console.WriteLine("=== PASSER COMMANDE SIMPLE ===");
            Console.Write("Nom du produit : ");
            string produit = Console.ReadLine();
            Console.Write("Quantité : ");
            int quantite = int.Parse(Console.ReadLine());

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                // Vérifier produit
                string queryProduit = "SELECT ProduitId, PrixUnitaire, QuantiteStock FROM Produit WHERE Nom=@nom LIMIT 1";
                using (var cmdProduit = new SQLiteCommand(queryProduit, conn))
                {
                    cmdProduit.Parameters.AddWithValue("@nom", produit);
                    using (var reader = cmdProduit.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            Console.WriteLine("Produit introuvable !");
                            return;
                        }

                        int produitId = Convert.ToInt32(reader["ProduitId"]);
                        double prix = Convert.ToDouble(reader["PrixUnitaire"]);
                        int stock = Convert.ToInt32(reader["QuantiteStock"]);

                        if (quantite > stock)
                        {
                            Console.WriteLine("Quantité insuffisante !");
                            return;
                        }

                        // Créer la facture avec PersonnelId = 0 pour éviter l'erreur
                        string insertFacture = @"INSERT INTO Facture (DateFacture, ClientId, PersonnelId, Total)
                                             VALUES (datetime('now'), @clientId, 0, @total);
                                             SELECT last_insert_rowid();";
                        int factureId;
                        using (var cmdFacture = new SQLiteCommand(insertFacture, conn))
                        {
                            cmdFacture.Parameters.AddWithValue("@clientId", this.Id);
                            cmdFacture.Parameters.AddWithValue("@total", prix * quantite);
                            factureId = Convert.ToInt32(cmdFacture.ExecuteScalar());
                        }

                        // Ligne facture
                        string insertLigne = @"INSERT INTO LigneFacture (FactureId, ProduitId, Quantite, PrixTotal)
                                           VALUES (@factureId, @produitId, @quant, @prixTotal)";
                        using (var cmdLigne = new SQLiteCommand(insertLigne, conn))
                        {
                            cmdLigne.Parameters.AddWithValue("@factureId", factureId);
                            cmdLigne.Parameters.AddWithValue("@produitId", produitId);
                            cmdLigne.Parameters.AddWithValue("@quant", quantite);
                            cmdLigne.Parameters.AddWithValue("@prixTotal", prix * quantite);
                            cmdLigne.ExecuteNonQuery();
                        }

                        // Mettre à jour stock
                        string updateStock = "UPDATE Produit SET QuantiteStock = QuantiteStock - @qte WHERE ProduitId=@id";
                        using (var cmdStock = new SQLiteCommand(updateStock, conn))
                        {
                            cmdStock.Parameters.AddWithValue("@qte", quantite);
                            cmdStock.Parameters.AddWithValue("@id", produitId);
                            cmdStock.ExecuteNonQuery();
                        }

                        Console.WriteLine($"Commande passée : {quantite} x {produit}, Total = {prix * quantite}.");

                        // Générer PDF
                        string pdfPath = $"Facture_{factureId}.pdf";
                        using (var writer = new PdfWriter(pdfPath))
                        using (var pdf = new PdfDocument(writer))
                        {
                            var doc = new Document(pdf);
                            doc.Add(new Paragraph($"Facture ID : {factureId}"));
                            doc.Add(new Paragraph($"Client : {Prenom} {Nom}"));
                            doc.Add(new Paragraph($"Produit : {produit}"));
                            doc.Add(new Paragraph($"Quantité : {quantite}"));
                            doc.Add(new Paragraph($"Prix unitaire : {prix}"));
                            doc.Add(new Paragraph($"Total : {prix * quantite}"));
                            doc.Close();
                        }

                        Console.WriteLine($"PDF généré : {pdfPath}");
                    }
                }
            }
        }
    }

    // ===========================
    // ClientEntreprise
    // ===========================
    public class ClientEntreprise : Consommateur
    {
        public void PasserCommandeEntreprise()
        {
            Console.WriteLine("=== PASSER COMMANDE ENTREPRISE ===");
            Console.Write("Nom du produit : ");
            string produit = Console.ReadLine();
            Console.Write("Quantité : ");
            int quantite = int.Parse(Console.ReadLine());

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                string queryProduit = "SELECT ProduitId, PrixUnitaire, QuantiteStock FROM Produit WHERE Nom=@nom LIMIT 1";
                using (var cmdProduit = new SQLiteCommand(queryProduit, conn))
                {
                    cmdProduit.Parameters.AddWithValue("@nom", produit);
                    using (var reader = cmdProduit.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            Console.WriteLine("Produit introuvable !");
                            return;
                        }

                        int produitId = Convert.ToInt32(reader["ProduitId"]);
                        double prix = Convert.ToDouble(reader["PrixUnitaire"]);
                        int stock = Convert.ToInt32(reader["QuantiteStock"]);

                        if (quantite > stock)
                        {
                            Console.WriteLine("Quantité insuffisante !");
                            return;
                        }

                        string insertFacture = @"INSERT INTO Facture (DateFacture, ClientId, PersonnelId, Total)
                                             VALUES (datetime('now'), @clientId, 0, @total);
                                             SELECT last_insert_rowid();";
                        int factureId;
                        using (var cmdFacture = new SQLiteCommand(insertFacture, conn))
                        {
                            cmdFacture.Parameters.AddWithValue("@clientId", this.Id);
                            cmdFacture.Parameters.AddWithValue("@total", prix * quantite);
                            factureId = Convert.ToInt32(cmdFacture.ExecuteScalar());
                        }

                        string insertLigne = @"INSERT INTO LigneFacture (FactureId, ProduitId, Quantite, PrixTotal)
                                           VALUES (@factureId, @produitId, @quant, @prixTotal)";
                        using (var cmdLigne = new SQLiteCommand(insertLigne, conn))
                        {
                            cmdLigne.Parameters.AddWithValue("@factureId", factureId);
                            cmdLigne.Parameters.AddWithValue("@produitId", produitId);
                            cmdLigne.Parameters.AddWithValue("@quant", quantite);
                            cmdLigne.Parameters.AddWithValue("@prixTotal", prix * quantite);
                            cmdLigne.ExecuteNonQuery();
                        }

                        string updateStock = "UPDATE Produit SET QuantiteStock = QuantiteStock - @qte WHERE ProduitId=@id";
                        using (var cmdStock = new SQLiteCommand(updateStock, conn))
                        {
                            cmdStock.Parameters.AddWithValue("@qte", quantite);
                            cmdStock.Parameters.AddWithValue("@id", produitId);
                            cmdStock.ExecuteNonQuery();
                        }

                        Console.WriteLine($"Commande entreprise passée : {quantite} x {produit}, Total = {prix * quantite}.");

                        string pdfPath = $"Facture_{factureId}.pdf";
                        using (var writer = new PdfWriter(pdfPath))
                        using (var pdf = new PdfDocument(writer))
                        {
                            var doc = new Document(pdf);
                            doc.Add(new Paragraph($"Facture ID : {factureId}"));
                            doc.Add(new Paragraph($"Client : {Prenom} {Nom}"));
                            doc.Add(new Paragraph($"Produit : {produit}"));
                            doc.Add(new Paragraph($"Quantité : {quantite}"));
                            doc.Add(new Paragraph($"Prix unitaire : {prix}"));
                            doc.Add(new Paragraph($"Total : {prix * quantite}"));
                            doc.Close();
                        }

                        Console.WriteLine($"PDF généré : {pdfPath}");
                    }
                }
            }
        }
    }

    // ===========================
    // ClientVIP
    // ===========================
    public class ClientVIP : Consommateur
    {
        public void PasserCommandeVIP()
        {
            Console.WriteLine("=== PASSER COMMANDE VIP ===");
            Console.Write("Nom du produit : ");
            string produit = Console.ReadLine();
            Console.Write("Quantité : ");
            int quantite = int.Parse(Console.ReadLine());
            double remise = 0.1; // 10% de remise

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                string queryProduit = "SELECT ProduitId, PrixUnitaire, QuantiteStock FROM Produit WHERE Nom=@nom LIMIT 1";
                using (var cmdProduit = new SQLiteCommand(queryProduit, conn))
                {
                    cmdProduit.Parameters.AddWithValue("@nom", produit);
                    using (var reader = cmdProduit.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            Console.WriteLine("Produit introuvable !");
                            return;
                        }

                        int produitId = Convert.ToInt32(reader["ProduitId"]);
                        double prix = Convert.ToDouble(reader["PrixUnitaire"]);
                        int stock = Convert.ToInt32(reader["QuantiteStock"]);

                        if (quantite > stock)
                        {
                            Console.WriteLine("Quantité insuffisante !");
                            return;
                        }

                        double total = prix * quantite * (1 - remise);

                        string insertFacture = @"INSERT INTO Facture (DateFacture, ClientId, PersonnelId, Total)
                                             VALUES (datetime('now'), @clientId, 0, @total);
                                             SELECT last_insert_rowid();";
                        int factureId;
                        using (var cmdFacture = new SQLiteCommand(insertFacture, conn))
                        {
                            cmdFacture.Parameters.AddWithValue("@clientId", this.Id);
                            cmdFacture.Parameters.AddWithValue("@total", total);
                            factureId = Convert.ToInt32(cmdFacture.ExecuteScalar());
                        }

                        string insertLigne = @"INSERT INTO LigneFacture (FactureId, ProduitId, Quantite, PrixTotal)
                                           VALUES (@factureId, @produitId, @quant, @prixTotal)";
                        using (var cmdLigne = new SQLiteCommand(insertLigne, conn))
                        {
                            cmdLigne.Parameters.AddWithValue("@factureId", factureId);
                            cmdLigne.Parameters.AddWithValue("@produitId", produitId);
                            cmdLigne.Parameters.AddWithValue("@quant", quantite);
                            cmdLigne.Parameters.AddWithValue("@prixTotal", total);
                            cmdLigne.ExecuteNonQuery();
                        }

                        string updateStock = "UPDATE Produit SET QuantiteStock = QuantiteStock - @qte WHERE ProduitId=@id";
                        using (var cmdStock = new SQLiteCommand(updateStock, conn))
                        {
                            cmdStock.Parameters.AddWithValue("@qte", quantite);
                            cmdStock.Parameters.AddWithValue("@id", produitId);
                            cmdStock.ExecuteNonQuery();
                        }

                        Console.WriteLine($"Commande VIP passée : {quantite} x {produit}, Total = {total}");

                        string pdfPath = $"Facture_{factureId}.pdf";
                        using (var writer = new PdfWriter(pdfPath))
                        using (var pdf = new PdfDocument(writer))
                        {
                            var doc = new Document(pdf);
                            doc.Add(new Paragraph($"Facture ID : {factureId}"));
                            doc.Add(new Paragraph($"Client : {Prenom} {Nom}"));
                            doc.Add(new Paragraph($"Produit : {produit}"));
                            doc.Add(new Paragraph($"Quantité : {quantite}"));
                            doc.Add(new Paragraph($"Prix unitaire : {prix}"));
                            doc.Add(new Paragraph($"Total après remise : {total}"));
                            doc.Close();
                        }

                        Console.WriteLine($"PDF généré : {pdfPath}");
                    }
                }
            }
        }

        public void BeneficierRemise()
        {
            Console.WriteLine("Remise VIP appliquée.");
        }
    }



    
    internal class Program
    {

        static void AfficherProduitsDisponibles()
        {
            using (var conn = new SQLiteConnection($"Data Source={Utilisateur.dbPath};Version=3;"))
            {
                conn.Open();
                string query = "SELECT Nom, Description, PrixUnitaire, QuantiteStock FROM Produit";
                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("\n=== PRODUITS DISPONIBLES ===");
                    Console.WriteLine("{0,-20} | {1,-30} | {2,10} | {3,10}", "Nom", "Description", "Prix", "Stock");
                    Console.WriteLine(new string('-', 80));

                    while (reader.Read())
                    {
                        string nom = reader["Nom"].ToString();
                        string desc = reader["Description"].ToString();
                        double prix = Convert.ToDouble(reader["PrixUnitaire"]);
                        int stock = Convert.ToInt32(reader["QuantiteStock"]);

                        Console.WriteLine("{0,-20} | {1,-30} | {2,10} | {3,10}", nom, desc, prix, stock);
                    }
                }
            }
            Console.WriteLine();
        }


        static void Main(string[] args)
        {
            Console.WriteLine("=== GESTION DE STOCK (CONSOLE) ===\n");

            Console.Write("Nom d'utilisateur : ");
            string username = Console.ReadLine().Trim();

            Console.Write("Mot de passe : ");
            string password = Console.ReadLine().Trim();

            Utilisateur user = Authentifier(username, password);

            if (user != null)
            {
                Console.WriteLine($"\nBienvenue {user.Prenom} {user.Nom} ! (Rôle : {user.Role})\n");
                user.SeConnecter();
                AfficherProduitsDisponibles();
                AfficherMenuSelonRole(user);
                user.SeDeconnecter();
            }
            else
            {
                Console.WriteLine("\nNom d'utilisateur ou mot de passe incorrect.");
            }

            Console.WriteLine("\nAppuyez sur une touche pour quitter...");
            Console.ReadKey();
        }

        static Utilisateur Authentifier(string username, string password)
        {
            using (var conn = new SQLiteConnection($"Data Source={Utilisateur.dbPath};Version=3;"))
            {
                conn.Open();
                string query = @"SELECT UserId, Nom, Prenom, Role
                                 FROM Utilisateur
                                 WHERE LOWER(TRIM(Username)) = LOWER(TRIM(@user))
                                   AND TRIM(PasswordHash) = TRIM(@pass)
                                 LIMIT 1";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@user", username);
                    cmd.Parameters.AddWithValue("@pass", password);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int id = Convert.ToInt32(reader["UserId"]);
                            string nom = reader["Nom"].ToString();
                            string prenom = reader["Prenom"].ToString();
                            string role = reader["Role"].ToString();

                            switch (role)
                            {
                                case "AdminProduits": return new AdminProduits { Id = id, Nom = nom, Prenom = prenom, Username = username, Role = role };
                                case "AdminClients": return new AdminClients { Id = id, Nom = nom, Prenom = prenom, Username = username, Role = role };
                                case "AdminTransactions": return new AdminTransactions { Id = id, Nom = nom, Prenom = prenom, Username = username, Role = role };
                                case "AdminSysteme": return new AdminSysteme { Id = id, Nom = nom, Prenom = prenom, Username = username, Role = role };

                                case "Caissier": return new Caissier { Id = id, Nom = nom, Prenom = prenom, Username = username, Role = role };
                                case "GestionnaireStock": return new GestionnaireStock { Id = id, Nom = nom, Prenom = prenom, Username = username, Role = role };
                                case "ResponsableVentes": return new ResponsableVentes { Id = id, Nom = nom, Prenom = prenom, Username = username, Role = role };
                                case "AssistantBoutique": return new AssistantBoutique { Id = id, Nom = nom, Prenom = prenom, Username = username, Role = role };

                                case "ClientSimple": return new ClientSimple { Id = id, Nom = nom, Prenom = prenom, Username = username, Role = role };
                                case "ClientEntreprise": return new ClientEntreprise { Id = id, Nom = nom, Prenom = prenom, Username = username, Role = role };
                                case "ClientVIP": return new ClientVIP { Id = id, Nom = nom, Prenom = prenom, Username = username, Role = role };

                                default: return null;
                            }
                        }
                    }
                }
            }
            return null;
        }

        static void AfficherMenuSelonRole(Utilisateur user)
        {
            // ================= Admin Produits =================
            if (user is AdminProduits ap)
            {
                while (true)
                {
                    Console.WriteLine("=== MENU ADMIN PRODUITS ===");
                    Console.WriteLine("1 - Ajouter produit");
                    Console.WriteLine("2 - Modifier produit");
                    Console.WriteLine("3 - Supprimer produit");
                    Console.WriteLine("4 - Quitter");
                    Console.Write("Choix : ");
                    string choix = Console.ReadLine();

                    switch (choix)
                    {
                        case "1": ap.AjouterProduit(); break;
                        case "2": ap.ModifierProduit(); break;
                        case "3": ap.SupprimerProduit(); break;
                        case "4": return;
                        default: Console.WriteLine("Choix invalide."); break;
                    }
                }
            }

            // ================= Admin Clients =================
            else if (user is AdminClients ac)
            {
                while (true)
                {
                    Console.WriteLine("=== MENU ADMIN CLIENTS ===");
                    Console.WriteLine("1 - Ajouter client");
                    Console.WriteLine("2 - Modifier client");
                    Console.WriteLine("3 - Supprimer client");
                    Console.WriteLine("4 - Quitter");
                    Console.Write("Choix : ");
                    string choix = Console.ReadLine();

                    switch (choix)
                    {
                        case "1": ac.AjouterClient(); break;
                        case "2": ac.ModifierClient(); break;
                        case "3": ac.SupprimerClient(); break;
                        case "4": return;
                        default: Console.WriteLine("Choix invalide."); break;
                    }
                }
            }

            // ================= Admin Transactions =================
            else if (user is AdminTransactions at)
            {
                while (true)
                {
                    Console.WriteLine("=== MENU ADMIN TRANSACTIONS ===");
                    Console.WriteLine("1 - Lister transactions");
                    Console.WriteLine("2 - Quitter");
                    Console.Write("Choix : ");
                    string choix = Console.ReadLine();

                    switch (choix)
                    {
                        case "1": at.ListerTransactions(); break;
                        case "2": return;
                        default: Console.WriteLine("Choix invalide."); break;
                    }
                }
            }

            // ================= Admin Système =================
            else if (user is AdminSysteme asys)
            {
                while (true)
                {
                    Console.WriteLine("=== MENU ADMIN SYSTÈME ===");
                    Console.WriteLine("1 - Sauvegarder base");
                    Console.WriteLine("2 - Quitter");
                    Console.Write("Choix : ");
                    string choix = Console.ReadLine();

                    switch (choix)
                    {
                        case "1": asys.SauvegarderBD(); break;
                        case "2": return;
                        default: Console.WriteLine("Choix invalide."); break;
                    }
                }
            }

            // ================= Personnel =================
            else if (user is Caissier c)
            {
                while (true)
                {
                    Console.WriteLine("=== MENU CAISSIER ===");
                    Console.WriteLine("1 - Créer facture");
                    Console.WriteLine("2 - Encaisser");
                    Console.WriteLine("3 - Quitter");
                    Console.Write("Choix : ");
                    string choix = Console.ReadLine();

                    switch (choix)
                    {
                        case "1": c.CreerFacture(); break;
                        case "2": c.Encaisser(); break;
                        case "3": return;
                        default: Console.WriteLine("Choix invalide."); break;
                    }
                }
            }
            else if (user is GestionnaireStock g)
            {
                while (true)
                {
                    Console.WriteLine("=== MENU GESTIONNAIRE STOCK ===");
                    Console.WriteLine("1 - Mettre à jour stock");
                    Console.WriteLine("2 - Réceptionner produit");
                    Console.WriteLine("3 - Quitter");
                    Console.Write("Choix : ");
                    string choix = Console.ReadLine();

                    switch (choix)
                    {
                        case "1": g.MettreAJourStock(); break;
                        case "2": g.ReceptionnerProduit(); break;
                        case "3": return;
                        default: Console.WriteLine("Choix invalide."); break;
                    }
                }
            }
            else if (user is ResponsableVentes rv)
            {
                while (true)
                {
                    Console.WriteLine("=== MENU RESPONSABLE VENTES ===");
                    Console.WriteLine("1 - Superviser ventes");
                    Console.WriteLine("2 - Quitter");
                    Console.Write("Choix : ");
                    string choix = Console.ReadLine();

                    switch (choix)
                    {
                        case "1": rv.SuperviserVentes(); break;
                        case "2": return;
                        default: Console.WriteLine("Choix invalide."); break;
                    }
                }
            }
            else if (user is AssistantBoutique ab)
            {
                while (true)
                {
                    Console.WriteLine("=== MENU ASSISTANT BOUTIQUE ===");
                    Console.WriteLine("1 - Accueil client");
                    Console.WriteLine("2 - Quitter");
                    Console.Write("Choix : ");
                    string choix = Console.ReadLine();

                    switch (choix)
                    {
                        case "1": ab.AccueilClient(); break;
                        case "2": return;
                        default: Console.WriteLine("Choix invalide."); break;
                    }
                }
            }

            // ================= Clients =================
            else if (user is ClientSimple cs)
            {
                while (true)
                {
                    Console.WriteLine("=== MENU CLIENT SIMPLE ===");
                    Console.WriteLine("1 - Passer commande");
                    Console.WriteLine("2 - Quitter");
                    Console.Write("Choix : ");
                    string choix = Console.ReadLine();

                    switch (choix)
                    {
                        case "1": cs.PasserCommande();  break;
                        case "2": return;
                        default: Console.WriteLine("Choix invalide."); break;
                    }
                }
            }
            else if (user is ClientEntreprise ce)
            {
                while (true)
                {
                    Console.WriteLine("=== MENU CLIENT ENTREPRISE ===");
                    Console.WriteLine("1 - Passer commande entreprise");
                    Console.WriteLine("2 - Quitter");
                    Console.Write("Choix : ");
                    string choix = Console.ReadLine();

                    switch (choix)
                    {
                        case "1": ce.PasserCommandeEntreprise(); break;
                        case "2": return;
                        default: Console.WriteLine("Choix invalide."); break;
                    }
                }
            }
            else if (user is ClientVIP cv)
            {
                while (true)
                {
                    Console.WriteLine("=== MENU CLIENT VIP ===");
                    Console.WriteLine("1 - Passer commande VIP");
                    Console.WriteLine("2 - Bénéficier remise");
                    Console.WriteLine("3 - Quitter");
                    Console.Write("Choix : ");
                    string choix = Console.ReadLine();

                    switch (choix)
                    {
                        case "1": cv.PasserCommandeVIP(); break;
                        case "2": cv.BeneficierRemise(); break;
                        case "3": return;
                        default: Console.WriteLine("Choix invalide."); break;
                    }
                }
            }
            else
            {
                Console.WriteLine("Rôle non reconnu ou menu non implémenté.");
            }
        }
    }
}
