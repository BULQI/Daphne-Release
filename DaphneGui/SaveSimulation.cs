using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daphne;
using ManifoldRing;
using System.IO;
using System.Windows.Markup;
using System.Windows.Data;

namespace DaphneGui
{
    public partial class MainWindow
    {


        internal static SimConfigurator SimConfigSaver = null;
        internal static string filepath_prefix = null;
        internal static int save_conter = 1;

        /// <summary>
        /// save simulation state, enabled when simulaiton is paused.
        /// </summary>
        public void save_simululation_state()
        {
            runButton.IsEnabled = false;
            resetButton.IsEnabled = false;
            if (SimConfigSaver == null)
            {
                //copy initial settings, only done once
                SimConfigSaver = new SimConfigurator();
                SimConfigSaver.DeserializeSimConfigFromString(configurator.SerializeSimConfigToString());
                string sp = configurator.FileName;
                filepath_prefix = Path.Combine(System.IO.Path.GetDirectoryName(sp), System.IO.Path.GetFileNameWithoutExtension(sp));
            }
            else
            {
                //clear the contents from last save
                foreach (KeyValuePair<int, CellPopulation> item in SimConfigSaver.SimConfig.cellpopulation_id_cellpopulation_dict)
                {
                    item.Value.cell_list.Clear();
                }
            }


            //get filename to save
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = orig_path;
            dlg.FileName = filepath_prefix + "-" + save_conter + ".json";
            dlg.DefaultExt = ".json"; // Default file extension
            dlg.Filter = "Sim State JSON docs (.json)|*.json"; // Filter files by extension

            // Show save file dialog box
            if (dlg.ShowDialog() != true)
            {
                runButton.IsEnabled = true;
                resetButton.IsEnabled = true;
                return;
            }
            SimConfigSaver.FileName = dlg.FileName;
            save_conter++;

            SimConfiguration save_config = SimConfigSaver.SimConfig;
            Scenario save_scenario = save_config.scenario;

            //same Simulation.dataBasket.ECS.Space.Population inot scenario.environmnet.ecs.molpop
            foreach (ConfigMolecularPopulation cmp in save_scenario.environment.ecs.molpops)
            {
                //loop through molecular population
                MolecularPopulation cur_mp = Simulation.dataBasket.ECS.Space.Populations[cmp.molecule_guid_ref];
                ScalarField molecule_concentration = cur_mp.Conc;

                double[] cur_conc_values = new double[molecule_concentration.ValueArray.Length];
                Array.Copy(molecule_concentration.ValueArray, cur_conc_values, cur_conc_values.Length);

                MolPopExplicit mpex = new MolPopExplicit();
                mpex.conc = cur_conc_values;
                cmp.mpInfo.mp_distribution = mpex;
            }

            //add cells into their respenctive populaiton
            foreach (KeyValuePair<int, Cell> kvp in Simulation.dataBasket.Cells)
            {
                Cell cell = kvp.Value;

                int cell_set_id = cell.Population_id;

                CellPopulation target_cp = SimConfigSaver.SimConfig.cellpopulation_id_cellpopulation_dict[cell.Population_id];
                CellState cell_state = new CellState();
                cell_state.setState(cell.State);

                Dictionary<string, MolecularPopulation> membrane_mol_pop_dict = cell.PlasmaMembrane.Populations;
                foreach (KeyValuePair<string, MolecularPopulation> kvpair in membrane_mol_pop_dict)
                {
                    MolecularPopulation mp = kvpair.Value;
                    cell_state.addMolPopulation(kvpair.Key, mp);
                }

                Dictionary<string, MolecularPopulation> cytosol_mol_pop_dict = cell.Cytosol.Populations;
                foreach (KeyValuePair<string, MolecularPopulation> kvpair in cytosol_mol_pop_dict)
                {
                    MolecularPopulation mp = kvpair.Value;
                    cell_state.addMolPopulation(kvpair.Key, mp);
                }
                target_cp.cell_list.Add(cell_state);
            }

            SimConfigSaver.SerializeSimConfigToFile();
            runButton.IsEnabled = true;
            resetButton.IsEnabled = true;
        }

    }

        public class PauseButtonTextToBoolConverter : MarkupExtension, IValueConverter
        {
            public static PauseButtonTextToBoolConverter _converter = null;

            //not needed, to avoid designer error
            public PauseButtonTextToBoolConverter() { }
            public PauseButtonTextToBoolConverter(object x) { }


            object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return value as string == "Continue";
            }

            object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            public override object ProvideValue(IServiceProvider serviceProvider)
            {
                if (_converter == null)
                {
                    _converter = new PauseButtonTextToBoolConverter();
                }
                return _converter;
            }
        }
    
}
