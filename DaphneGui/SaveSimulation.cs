using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using ManifoldRing;
using System.IO;
using System.Windows.Markup;
using System.Windows.Data;

using Daphne;

namespace DaphneGui
{
    public partial class MainWindow
    {
        internal static Protocol ProtocolSaver = null;
        internal static string filepath_prefix = null;
        internal static int save_counter = 1;

        /// <summary>
        /// save simulation state, enabled when simulaiton is paused.
        /// </summary>
        public void save_simulation_state()
        {
            // remember the current button enabled states
            bool[] buttons = new bool[3];
            int RUN = 0, RESET = 1, ABORT = 2;

            buttons[RUN] = runButton.IsEnabled;
            buttons[RESET] = applyButton.IsEnabled;
            buttons[ABORT] = abortButton.IsEnabled;

            runButton.IsEnabled = false;
            applyButton.IsEnabled = false;
            abortButton.IsEnabled = false;
            if (ProtocolSaver == null)
            {
                //copy initial settings, only done once
                ProtocolSaver = new Protocol("", orig_path + @"\temp_protocol.json", sop.Protocol.GetScenarioType());
            }
            SystemOfPersistence.DeserializeExternalProtocolFromString(ref ProtocolSaver, sop.Protocol.SerializeToString());
 
            //clear the contents from last save
            foreach (KeyValuePair<int, CellPopulation> item in ((TissueScenario)ProtocolSaver.scenario).cellpopulation_dict)
            {
                // item.Value.cell_list.Clear();
                item.Value.CellStates.Clear();
            }

            string sp = sop.Protocol.FileName;

            filepath_prefix = System.IO.Path.GetDirectoryName(sp);
            if (argSave == false)
            {
                //get filename to save
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

                // HS: don't know why the full path would be needed; observe for side-effects and if no issues remove
                //filepath_prefix = Path.Combine(filepath_prefix, System.IO.Path.GetFileNameWithoutExtension(sp));
                filepath_prefix = System.IO.Path.GetFileNameWithoutExtension(sp);
                dlg.InitialDirectory = orig_path;
                dlg.FileName = filepath_prefix + "-" + save_counter + ".json";
                dlg.DefaultExt = ".json"; // Default file extension
                dlg.Filter = "Sim State JSON docs (.json)|*.json"; // Filter files by extension

                // Show save file dialog box
                if (dlg.ShowDialog() != true)
                {
                    runButton.IsEnabled = buttons[RUN];
                    applyButton.IsEnabled = buttons[RESET];
                    abortButton.IsEnabled = buttons[ABORT];
                    return;
                }
                ProtocolSaver.FileName = dlg.FileName;
            }
            else
            {
                if (MainWindow.Sim.Reporter.FileNameBase != "")
                {
                    filepath_prefix = Path.Combine(filepath_prefix, MainWindow.Sim.Reporter.FileNameBase);
                }
                else
                {
                    filepath_prefix = Path.Combine(filepath_prefix, System.IO.Path.GetFileNameWithoutExtension(sp)) + "-" + save_counter;
                }
                ProtocolSaver.FileName = filepath_prefix + ".json";
            }

            save_counter++;

            //same Simulation.dataBasket.ECS.Comp.Population inot scenario.environmnet.ecs.molpop
            foreach (ConfigMolecularPopulation cmp in ProtocolSaver.scenario.environment.comp.molpops)
            {
                //loop through molecular population
                MolecularPopulation cur_mp = SimulationBase.dataBasket.Environment.Comp.Populations[cmp.molecule.entity_guid];

                MolPopExplicit mpex = new MolPopExplicit();
                double[] cur_conc_values = new double[cur_mp.Conc.M.ArraySize];
                cur_mp.Conc.CopyArray(cur_conc_values);
                mpex.conc = cur_conc_values;
                mpex.Description = "saved state";

                cmp.mp_distribution = mpex;
            }

            foreach (var kvp in ((TissueScenario)ProtocolSaver.scenario).cellpopulation_dict)
            {
                CellPopulation cp = kvp.Value;
                cp.number = 0;
            }

            //add cells into their respenctive populaiton
            foreach (KeyValuePair<int, Cell> kvp in SimulationBase.dataBasket.Cells)
            {
                Cell cell = kvp.Value;
                CellPopulation target_cp = ((TissueScenario)ProtocolSaver.scenario).cellpopulation_dict[cell.Population_id];
                CellState cell_state = new CellState();

                target_cp.number++;

                cell_state.setSpatialState(cell.SpatialState);
                cell_state.setDeathDriverState(cell.DeathBehavior);
                cell_state.setDivisonDriverState(cell.Divider.Behavior);
                cell_state.setDifferentiationDriverState(cell.Differentiator.Behavior);

                cell_state.setGeneState(cell.Genes);
                cell_state.CellGeneration = cell.generation;
                cell_state.Cell_id = cell.Cell_id;
                cell_state.Lineage_id = cell.Lineage_id.ToString();

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

                if (SimulationBase.cellManager.DeadDict.ContainsKey(cell.Cell_id) == true)
                {
                    cell_state.setRemovalState(SimulationBase.cellManager.DeadDict[cell.Cell_id]);
                }

                target_cp.CellStates.Add(cell_state);
                //target_cp.cell_list.Add(cell_state);
            }

            ProtocolSaver.sim_params.globalRandomSeed = Daphne.Rand.MersenneTwister.Next();

            ProtocolSaver.SerializeToFile();
            runButton.IsEnabled = buttons[RUN];
            applyButton.IsEnabled = buttons[RESET];
            abortButton.IsEnabled = buttons[ABORT];
        }

    }

    public class SimulationStateToBoolConverter : MarkupExtension, IValueConverter
    {
        public static SimulationStateToBoolConverter _converter = null;

        //not needed, to avoid designer error
        public SimulationStateToBoolConverter() { }
        public SimulationStateToBoolConverter(object x) { }


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (byte)value == SimulationBase.RUNSTAT_PAUSE || (byte)value == SimulationBase.RUNSTAT_FINISHED;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (_converter == null)
            {
                _converter = new SimulationStateToBoolConverter();
            }
            return _converter;
        }
    }
    
}
