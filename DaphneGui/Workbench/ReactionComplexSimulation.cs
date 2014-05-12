using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daphne;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using System.Xml;

using DaphneGui;
using ManifoldRing;


namespace Workbench
{
    public class ReactionComplexSimulation
    {
        public MainWindow MW { get; set; }        

        private DaphneGui.ReactionsConfigurator config;
        private Dictionary<string, Molecule> MolDict;
        public Dictionary<string, Molecule> MolecDict
        {
            get
            {
                return MolDict;
            }
            set
            {
                MolDict = value;
            }
        }

        //These are for graphing the concentrations.  This data is generated during the Go method.
        protected List<double> listTimes = new List<double>();
        public List<double> ListTimes
        {
            get
            {
                return listTimes;
            }
            set
            {
                listTimes = value;
            }
        }

        protected Dictionary<string, List<double>> dictGraphConcs = new Dictionary<string, List<double>>();

        //Molecular populations
        //private List<MolecularPopulation> totalMolPop = new List<MolecularPopulation>();
        //public List<MolecularPopulation> TotalMolPop
        //{
        //    get
        //    {
        //        return totalMolPop;
        //    }
        //    set
        //    {
        //        totalMolPop = value;
        //    }
        //}

        private ObservableCollection<MolecularPopulation> obsMolPop = new ObservableCollection<MolecularPopulation>();
        public ObservableCollection<MolecularPopulation> ObsMolPop
        {
            get
            {
                return obsMolPop;
            }
            set
            {
                obsMolPop = value;
            }
        }

        //Reactions
        private List<GuiReactionTemplate> rtList = new List<GuiReactionTemplate>();
        public List<GuiReactionTemplate> ReactionTemplateList
        {
            get
            {
                return rtList;
            }
            set
            {
                rtList = value;
            }
        }

        //Reaction Complex
        private ReactionComplex rc = null;
        public ReactionComplex RC
        {
            get
            {
                return rc;
            }
            set
            {
                rc = value;
            }
        }

        
        public ReactionComplexSimulation()
        {
        }

        public ReactionComplexSimulation(GuiReactionComplex grc)
        {
            rc = new ReactionComplex(grc.Name, new TinyBall(5.0));
            
            grc.CopyReactionsTo(rc);
            grc.ParseForMolecules();
            //grc.CopyMoleculesTo(rc);
            rc.Initialize();

            foreach (KeyValuePair<string, Molecule> kvp in grc.MolDict)
            {
                rc.AddMolecularPopulation(kvp.Value, 2.0);
            }

            foreach (GuiReactionTemplate grt in rc.ReactionsInComplex)
            {
                ReactionTemplate rt = new ReactionTemplate();
                grt.CopyTo(rt);
                ReactionBuilder.ReactionSwitch(rc, rt);
            }

            foreach (KeyValuePair<string, MolecularPopulation> kvp in rc.Populations)
            {
                //totalMolPop.Add(kvp.Value);
                ObsMolPop.Add(kvp.Value);
            }

            rc.SaveOriginalConcs();
            rc.SaveInitialConcs();
            
        }

        public ReactionComplexSimulation(MainWindow mw)
        {
            MW = mw;            
        }

        public void Go()
        {
            if (rc != null)
                rc.Go();
        }

        public void Initialize()
        {
            // Format:  Name1\tMolWt1\tEffRad1\tDiffCoeff1\nName2\tMolWt2\tEffRad2\tDiffCoeff2\n...
            //string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t\nCXCR5:CXCL13\t\t\t\ngCXCR5\t\t\t\n";
            string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t\nCXCR5:CXCL13\t\t\t\ngCXCR5\t\t\t\nE\t\t\t\nA\t1.0\t2.0\t1.0\nX\t24.0\t48.0\t1.0\nY\t25.0\t50.0\t1.0\nP\t\t\t\nS\t\t\t\n";
            MolDict = MoleculeBuilder.Go(molSpec);
            
            //THIS STORES ALL REACTIONS INTO THE MAIN LIST
            ReactionTemplateList.AddRange(config.content.listOfReactions);

            //--------------------------------------------------------------------------------------------

            //string filepath = @"c:\temp\reactions.xml";
            //config.serialize(filepath);
            //config.deserialize(filepath);

            
            ////Start Json Testing
            ////Serialize config variable to a json text file - This part works
            ////JsonSerializer serializer = new JsonSerializer();
            ////using (StreamWriter sw = new StreamWriter(@"c:\temp\json_output.txt"))
            ////using (JsonWriter writer = new JsonTextWriter(sw))
            ////{
            ////    serializer.Serialize(writer, config.content.listOfReactions);
            ////}

            ////////This deserializes what we just serialized above, back into config variable
            ////string path = @"c:\temp\json_output.txt";
            ////string readText = File.ReadAllText(path);
            ////config = JsonConvert.DeserializeObject<ReactionsConfigurator>(readText);            
            ////End of working part


            //  NEW TEST
            //string jsonText = JsonConvert.SerializeObject(config);
            ////XMLReactionsSpec xxx = JsonConvert.DeserializeObject<XMLReactionsSpec>(output);
            //XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(jsonText);
            //string myxml = doc.InnerXml;
            //path = @"c:\temp\json_xml.xml";
            //File.WriteAllText(path, myxml);

            ////jsonText = File.ReadAllText(path); 
            //jsonText = JsonConvert.SerializeXmlNode(doc);
            //config.content = JsonConvert.DeserializeObject<XMLReactionsSpec>(jsonText); 


            
            //TextWriter
            //XmlTextWriter w = new XmlTextWriter(tw);
            //doc.WriteTo(w);

            //--------------------------------------------------------------------------------------



            ////Write out just config.content and read it back in to XMLReactionsSpec - This works fine too!
            //JsonSerializer serializer = new JsonSerializer();
            //using (StreamWriter sw = new StreamWriter(@"c:\temp\json_output_whole.txt"))
            //using (JsonWriter writer = new JsonTextWriter(sw))
            //{
            //    serializer.Serialize(writer, config.content);
            //}
            //string path = @"c:\temp\json_output_whole.txt";
            //string readText = File.ReadAllText(path);
            //XMLReactionsSpec XR = JsonConvert.DeserializeObject<XMLReactionsSpec>(readText);
            //XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(output);
            //path = @"c:\temp\json_xml.xml";
            //string content = doc.InnerXml;
            //File.WriteAllText(path, content);


            ////PROBLEMS START HAPPENING WHEN WE CONVERT (SERIALIZE) THE XML TO JSON TEXT AND THEN TRY TO DESERIALIZE THE JSON TEXT INTO C# OBJECTS.

            ////Test Json XML to JSON conversion
            //string path2 = "ReacSpecFile1_copy.xml";
            //string readText2 = File.ReadAllText(path2);
            //XmlDocument doc = new XmlDocument();
            //doc.LoadXml(readText2);
            //string jsonText = JsonConvert.SerializeXmlNode(doc);
            //path2 = "ReacSpecFile1_Json.txt";            
            //File.WriteAllText(path2, jsonText);
            ////FINE UP TO HERE - CONVERTED XML TO JSON AND WROTE IT OUT            

            ////THIS CODE JUST DESERIALIZES JSON BACK INTO XmlDocument
            ////XmlDocument doc2 = (XmlDocument)JsonConvert.DeserializeXmlNode(jsonText);
            ////string xmlString = doc2.InnerXml;
            ////path2 = "RSF-Copy.xml";
            ////File.WriteAllText(path, xmlString);


            ////DESERIALIZE TO C# DOES NOT WORK - NOT ABLE TO READ JSON TEXT (THAT WAS CONVERTED FROM XML) INTO XMLReactionsSpec!!!!!
            ////path2 = "ReacSpecFile1_Json_converted_online2.txt";
            ////readText2 = File.ReadAllText(path2);
            ////Newtonsoft.Json.Linq.JObject jObject = Newtonsoft.Json.Linq.JObject.Parse(jsonText);
            ////ReactionTemplate rt = (ReactionTemplate)(jObject["listOfReactions"][0]);
            //XMLReactionsSpec XRS = JsonConvert.DeserializeObject<XMLReactionsSpec>(jsonText);
            ////config.content = JsonConvert.DeserializeObject<XMLReactionsSpec>(readText2);            
            ////List<test> myDeserializedObjList = (List<test>)Newtonsoft.Json.JsonConvert.DeserializeObject(Request["jsonString"], typeof(List<test>));
            ////List<ReactionTemplate> myObjList = (List<ReactionTemplate>)JsonConvert.DeserializeObject(jsonText, typeof(List<ReactionTemplate>));            
            ////XMLReactionsSpec XRS = (XMLReactionsSpec)JsonConvert.DeserializeObject(jsonText, typeof(XMLReactionsSpec));
            ////ReactionTemplateList.Clear();
            //ReactionTemplateList.AddRange(XRS.listOfReactions);

            ////End Json testing




            //Create some Reaction Complexes
            //LoadPredefinedReactionComplexes();

            //Load Diff Schemes
            //LoadDiffSchemes();

            //LinqTest();
            
        }
        

  
        

        //public void LoadPredefinedReactionComplexes()
        //{
        //    ObsMolPop.Clear();
        //    //TotalMolPop.Clear();

        //    //----------------------------------------------------------------------------------

        //    ReactionComplex rc = new ReactionComplex("Receptor/Ligand", new TinyBall());

        //    rc.AddMolecularPopulation(MolDict["CXCR5"], 2.0);
        //    rc.AddMolecularPopulation(MolDict["CXCL13"], 2.0);
        //    rc.AddMolecularPopulation(MolDict["CXCR5:CXCL13"], 2.0);

        //    //Now add the actual reactions using ReactionBuilder class            
        //    ReactionBuilder.ReactionSwitch(rc, ReactionTemplateList[0]);
        //    ReactionBuilder.ReactionSwitch(rc, ReactionTemplateList[1]);
        //    ReactionBuilder.ReactionSwitch(rc, ReactionTemplateList[3]);
        //    ReactionBuilder.ReactionSwitch(rc, ReactionTemplateList[4]);

        //    RCList.Add(rc);            
        //    foreach (KeyValuePair<string, MolecularPopulation> kvp in rc.Populations)
        //    {
        //        totalMolPop.Add(kvp.Value);
        //        ObsMolPop.Add(kvp.Value);
        //    }

        //    rc.Initialize();
        //    rc.SaveOriginalConcs();
        //    rc.SaveInitialConcs();

        //    //----------------------------------------------------------------------------------

        //    rc = new ReactionComplex("Bistable", new TinyBall());
            
        //    rc.AddMolecularPopulation(MolDict["E"], 1.0);
        //    rc.AddMolecularPopulation(MolDict["S"], 2.12);
        //    //rc.AddMolecularPopulation(MolDict["P"], 2.24);
        //    rc.AddMolecularPopulation(MolDict["X"], 2.37);
        //    rc.AddMolecularPopulation(MolDict["Y"], 2.5);

        //    //Now add the actual reactions using ReactionBuilder class
        //    ReactionBuilder.ReactionSwitch(rc, ReactionTemplateList[9]);
        //    ReactionBuilder.ReactionSwitch(rc, ReactionTemplateList[10]);
        //    ReactionBuilder.ReactionSwitch(rc, ReactionTemplateList[11]);
        //    ReactionBuilder.ReactionSwitch(rc, ReactionTemplateList[12]);

        //    RCList.Add(rc);
        //    foreach (KeyValuePair<string, MolecularPopulation> kvp in rc.Populations)
        //    {
        //        totalMolPop.Add(kvp.Value);                
        //        ObsMolPop.Add(kvp.Value);
        //    }

        //    rc.Initialize();
        //    rc.SaveOriginalConcs();
        //    rc.SaveInitialConcs();
           
        //}

        //public void EditReactionComplex(ReactionComplex rc, List<ReactionTemplate> temps)
        //{
        //    rc.rtList.Clear();
        //    rc.Populations.Clear();
        //    rc.rtList.AddRange(temps);
        //    rc.LoadMolecules(MolDict);            
        //    rc.SaveOriginalConcs();
        //    rc.SaveInitialConcs();
        //}

        //public void AddReactionComplex(ReactionComplex rc, List<ReactionTemplate> temps)
        //{
        //    rc.rtList.AddRange(temps);
        //    rc.LoadMolecules(MolDict);
        //    //RCList.Add(rc);
        //    MainWindow.SC.SimConfig.PredefReactionComplexes.Add(rc);
        //    rc.SaveOriginalConcs();
        //    rc.SaveInitialConcs();
        //}

        //public ReactionComplex FindReactionComplex(string name)
        //{            
        //    foreach (ReactionComplex rc in RCList)
        //    {
        //        if (rc.Name == name)
        //            return rc;
        //    }
        //    return null;
        //}

        //public bool AddMolecule(Molecule mol)
        //{
        //    molecules.Add(mol);
        //    if (!MolecDict.ContainsKey(mol.Name))
        //        MolecDict.Add(mol.Name, mol);

        //    return true;
        //}

        public bool RemoveMolecule(MolecularPopulation mp)
        {
            //ObsMolPop.Remove(mp);
            return ObsMolPop.Remove(mp);            
        }

        //public bool RemoveMolecule(Molecule m)
        //{            
        //    return Molecules.Remove(m);
        //}

        //public bool AddReactionTemplate(ReactionTemplate rt)
        //{
        //    config.content.listOfReactions.Add(rt);
        //    config.TemplReacType(config.content.listOfReactions);
        //    ReactionTemplateList.Add(rt);
        //    return true;
        //}

        //public bool DeleteReactionTemplate(ReactionTemplate rt)
        //{
        //    config.content.listOfReactions.Remove(rt);
        //    return ReactionTemplateList.Remove(rt);            
        //}

        //public void CopyReactionTemplatesToConfig()
        //{
        //    config.content.listOfReactions.Clear();
        //    config.content.listOfReactions.AddRange(ReactionTemplateList);
        //    config.TemplReacType(config.content.listOfReactions);
        //    RCList.Clear();
        //    LoadPredefinedReactionComplexes();
        //}

        //private void LinqTest()
        //{
        //    Type myTypeObj = TotalMolPop[0].Man.GetType();
        //    var query =
        //        from mp in TotalMolPop.AsParallel()
        //        where mp.Man.GetType().Name == "TinyBall"
        //        select mp;

        //    foreach (MolecularPopulation mp in query)
        //    {
        //        string name = mp.Man.GetType().Name;
        //    }
        //}
        
    }    
}
