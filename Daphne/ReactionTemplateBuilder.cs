using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;


namespace Daphne
{
    public class SpeciesReference
    {
        public SpeciesReference()
        {
            species = "";
            stoichiometry = 0;
        }

        public string species;
        public int stoichiometry;
    }

    public class ReactionTemplate
    {
        public ReactionTemplate()
        {
            listOfReactants = new List<SpeciesReference>();
            listOfProducts = new List<SpeciesReference>();
            rateConst = 0;
        }

        public List<SpeciesReference> listOfReactants;
        public List<SpeciesReference> listOfProducts;
        public double rateConst;
    }

    public class XMLReactionsSpec
    {
        public XMLReactionsSpec()
        {
            listOfReactions = new List<ReactionTemplate>();
        }

        public List<ReactionTemplate> listOfReactions;
    }

    public class ReactionsConfigurator
    {
        public ReactionsConfigurator()
        {
        }

        public void deserialize(string filename)
        {
            XmlSerializer xs;

            xs = new XmlSerializer(typeof(XMLReactionsSpec));

            using (Stream s = File.OpenRead(filename))
            {
                // content
                content = (XMLReactionsSpec)xs.Deserialize(s);
            }
        }

        public void serialize(string filename)
        {
            XmlSerializer xs = new XmlSerializer(typeof(XMLReactionsSpec));

            using (Stream s = File.Create(filename))
            {
                xs.Serialize(s, content);
            }
        }

        public XMLReactionsSpec content;
    }
}

