using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PlanificApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
 

// ... dentro de tu clase MainWindow
public MainWindow()
        {
            InitializeComponent();
            ProbarConexionMongo();
        }


        private async void ProbarConexionMongo()
        {
            try
            {
                // 1. Conectamos al motor que iniciamos hoy en el disco D
                var client = new MongoClient("mongodb://localhost:27017");

                // 2. Accedemos (o creamos) la base de datos de tu proyecto
                var database = client.GetDatabase("PlanificAppDB");

                // 3. Comando de "Ping" para ver si responde
                var ping = await database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");

                Debug.WriteLine("------------------------------------------");
                Debug.WriteLine("¡CONEXIÓN EXITOSA CON MONGODB EN DISCO D!");
                Debug.WriteLine("------------------------------------------");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("------------------------------------------");
                Debug.WriteLine($"ERROR DE CONEXIÓN: {ex.Message}");
                Debug.WriteLine("------------------------------------------");
            }
        }
    }
}
