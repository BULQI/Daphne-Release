using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DaphneGui
{
    /// <summary>
    /// class holding the whole environment
    /// </summary>
    public class CellEnvironment : IDynamic
    {
        /// <summary>
        /// constructor
        /// </summary>
        public CellEnvironment()
        {
        }

        /// <summary>
        /// accessor for the chemokine
        /// </summary>
        public Chemokine Ckine
        {
            get { return chemokine; }
            set { chemokine = value; }
        }

        /// <summary>
        /// accessor for the molecular population
        /// </summary>
        //////////public ExtracellularMedium ExtMedium
        //////////{
        //////////    get { return extMedium; }
        //////////    set { extMedium = value; }
        //////////}

        public void Step(double dt)
        {
            //////////extMedium.Step(dt);
        }
        /// <summary>
        /// chemokine data object
        /// </summary>
        private Chemokine chemokine;

        /// <summary>
        /// molecular population data object
        /// </summary>
        //////////private ExtracellularMedium extMedium;
    }
}
