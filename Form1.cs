using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Qlik.Engine;
using System.IO;
using System.Text;

namespace CreateMeasureDimension
{
    public partial class Form1 : Form
    {
        ILocation location;
        IHub hub;
        IApp newApp;
        String fileURL;


        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string App = textBox1.Text;

            hub = location.Hub();

            newApp = CreateApp(App, hub, location);

            textBox3.Text = "App creata o se gia presente aperta correttamente!!";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Connect();
                textBox3.Text = "Connesso!!";
            }
            catch (Exception ex)
            {
                textBox3.Text = "General error. " + ex.Message;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                string App = textBox1.Text;
                newApp = CreateApp(App, hub, location);

                int counter = 0;
                string line;
                string[] campi;
                System.IO.StreamReader file = new System.IO.StreamReader(fileURL);
                while ((line = file.ReadLine()) != null)
                {
                    campi = line.Split(';');
                    if (campi[0] == "Dimension")
                    {
                        CreateDimension(newApp, campi[1], campi[3], campi[2]);
                    }
                    if (campi[0] == "Measure")
                    {
                        CreateMeasure(newApp, campi[1], campi[2], campi[3], campi[4]);
                    }                    
                    counter++;
                }
                file.Close();
                textBox3.Text = "Dimensioni & Misure Create!";
            }
            catch (Exception ex)
            {
                textBox3.Text = "General error. " + ex.Message;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fileURL = openFileDialog1.FileName;
                textBox2.Text = fileURL;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                DownLoadLayout(newApp);
                textBox3.Text = "Layout Estratto!";
            }
            catch (Exception ex)
            {
                textBox3.Text = "Cliccare prima su crea app";
            }
        }

        private void Connect()
        {
            string conn = File.ReadAllText("config.txt", Encoding.UTF8);
            var uri = new Uri(conn);
            location = Qlik.Engine.Location.FromUri(uri);
            location.AsNtlmUserViaProxy(proxyUsesSsl: false);
        }

        private static void DownLoadLayout(IApp App)
        {
            string s = "Tipo;ID;Etichetta;Valore;LabelExpression\n";
            
            IEnumerable<NxInfo> e = App.GetAllInfos();
            
            IEnumerator<NxInfo> i = e.GetEnumerator();

            while (i.MoveNext())
            {
                if (i.Current.Type == "dimension")
                {
                    GenericDimension gd = App.GetGenericDimension(i.Current.Id);
                    var dim = new NxLibraryDimensionDef();
                    dim = gd.GetGenericDimension();
                    IEnumerable<string> listLabel = dim.FieldLabels;
                    IEnumerator<string> Label = listLabel.GetEnumerator();
                    Label.MoveNext();
                    s = s + "Dimension;" + i.Current.Id + ";" + Label.Current + ";;";

                    IEnumerable<string> listField = dim.FieldDefs;
                    IEnumerator<string> Field = listField.GetEnumerator();
                    Field.MoveNext();

                    s = s + Field.Current + "\n";
                }

                if (i.Current.Type == "measure")
                {
                    GenericMeasure gm = App.GetGenericMeasure(i.Current.Id);
                    var mea = new NxLibraryMeasureDef();
                    mea = gm.GetGenericMeasure();
                    string listLabel = mea.Label;
                    string LabelExpression = mea.LabelExpression;
                    s = s + "Measure;" + i.Current.Id + ";" + listLabel + ";";
                    string exp = mea.Def;

                    s = s + exp.Replace("\n","") + ";" + LabelExpression +"\n";
                }
            }

            string strPath = Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory) + "\\layout.csv";

            byte[] bytes = Encoding.Default.GetBytes(s);
            s = Encoding.Default.GetString(bytes);

            try
            {
                File.Delete(strPath);
                File.AppendAllText(strPath, s);
            }
            catch
            {
                File.AppendAllText(strPath, s );
            }
            
            

        }


        private static IApp CreateApp(string AppName, IHub hub, ILocation location)
        {
            IEnumerable<IAppIdentifier> e = hub.GetAppList();

            IEnumerator<IAppIdentifier> i = e.GetEnumerator();

            IAppIdentifier AppIdentifier = null;

            IApp App = null;

            int ok = 1;

            while (i.MoveNext() && ok == 1)
            {
                if (i.Current.AppName == AppName)
                {
                    App = location.App(i.Current);
                    ok = 0;
                    Console.WriteLine("App " + AppName + " già presente ricarico i dati");
                }
            }

            if (App == null)
            {
                AppIdentifier = location.CreateAppWithName(AppName);
                App = location.App(AppIdentifier);
            }

            return App;
        }

        private static void CreateDimension(IApp App, string DimID, string DimField, string DimName)
        {
            try
            {
                GenericDimension gd = App.GetGenericDimension(DimID);
                //Libreria di definizione delle dimensioni, oggetto che contiene tutte le proprietà
                var dim = new QlikNxLibraryDimensionDef();

                //Oggetto type, valorizzato a "dimension"
                var type = new NxInfo();

                //Oggetto di tipo astratto META, il costruttore amplia la classe NxMetaDef (definito nello script)
                var meta = new META();

                //Aggiungo alla lista delle dimensioni la dimensione che voglio creare
                List<string> listF = new List<string>();
                listF.Add(DimField);

                List<string> listN = new List<string>();
                listN.Add(DimName);

                //Proprietà della dimensione
                dim.FieldDefs = listF;
                dim.FieldLabels = listN;
                dim.Grouping = 0;
                dim.title = DimName;

                type.Type = "dimension";
                type.Id = DimID;
                    
                //Nome della dimensione che appare nella App
                meta.title = DimName;
                meta.description = "Id: " + DimID;

                //Dichiarazione degli oggetti per definire la dimensione e le proprietà
                var prop = new GenericDimensionProperties();

                prop.MetaDef = meta;
                prop.Info = type;
                prop.Dim = dim;

                if (gd == null)
                {
                    App.CreateGenericDimension(prop);
                    App.DoSave();
                }
                else
                {
                    gd.SetProperties(prop);
                    App.DoSave();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("General error." + Environment.NewLine + ex.Message);
            }
        }

        private static void CreateMeasure(IApp App, string MeaID, string MeaName, string Measure, string LabelExpression)
        {
            try
            {
                GenericMeasure gm = App.GetGenericMeasure(MeaID);
                //Dichiarazione degli oggetti per definire la misura e le proprietà
                var prop = new GenericMeasureProperties();

                //Libreria di definizione delle misure, oggetto che contiene tutte le proprietà
                var mea = new MEASURE();

                //Oggetto type, valorizzato a "measure"
                var type = new NxInfo();

                //Oggetto di tipo astratto META, il costruttore amplia la classe NxMetaDef (definito nello script)
                var meta = new META();

                //Aggiungo alla lista delle misure la misura che voglio creare
                List<string> list = new List<string>();
                list.Add(MeaName);

                //Proprietà della dimensione
                mea.ActiveExpression = 0;
                mea.Def = Measure; //Formula
                mea.Label = MeaName; //Nome della Misura
                mea.Grouping = 0;
                mea.LabelExpression = LabelExpression;
                //mea.coloring.basecolor.color = "#633d0c";

                prop.Measure = mea;

                type.Type = "measure";
                type.Id = MeaID;
                prop.Info = type;

                //Nome della misura che appare nella App
                meta.title = MeaName;
                meta.description = "Id: " + MeaID;
                prop.MetaDef = meta;

                if (gm == null)
                {
                    App.CreateGenericMeasure(prop);
                    App.DoSave();
                }
                else
                {
                    gm.SetProperties(prop);
                    App.DoSave();
                }             
            }
            catch (Exception ex)
            {
                Console.WriteLine("General error." + Environment.NewLine + ex.Message);
            }
        }
    }

    class BASECOLOR : COLOR
    {
        public string color
        {
            get => Get<string>("color"); set { Set("color", value); }
        }

    }

    class COLOR : MEASURE
    {
        public BASECOLOR basecolor
        {
            get => Get<BASECOLOR>("basecolor"); set { Set("basecolor", value); }
        }
    }

    class MEASURE : NxLibraryMeasureDef
    {
        public COLOR coloring
        {
            get => Get<COLOR>("coloring"); set { Set("coloring", value); }
        }
    }

    class META : NxMetaDef
    {
        public string title
        {
            get => Get<string>("title"); set { Set("title", value); }
        }

        public string description
        {
            get => Get<string>("description"); set { Set("description", value); }
        }
    }

    class QlikNxLibraryDimensionDef : NxLibraryDimensionDef
    {
        public string title
        {
            get => Get<string>("title"); set { Set("title", value); }
        }

        public string description
        {
            get => Get<string>("description"); set { Set("description", value); }
        }
    }
}
