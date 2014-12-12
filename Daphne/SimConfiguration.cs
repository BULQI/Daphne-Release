using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

using System.Xml.Serialization;
using Newtonsoft.Json;
using System.Linq;

using System.Windows.Media;
using System.Windows.Data;
using System.Windows;
using System.Windows.Markup;

using MathNet.Numerics.Distributions;
using MathNet.Numerics.Random;


namespace Daphne
{
    /// <summary>
    /// ties together all levels of storage
    /// </summary>
    public class SystemOfPersistence
    {
        /// <summary>
        /// Protocol level, contains Entity level
        /// </summary>
        public Protocol Protocol { get; set; }
        /// <summary>
        /// Daphne level
        /// </summary>
        public Level DaphneStore { get; set; }
        /// <summary>
        /// User level
        /// </summary>
        public Level UserStore { get; set; }


        public ObservableCollection<RenderSkin> SkinList { get; set; }
        /// <summary>
        /// The main palette containing graphics properties used to render various objects (i.e. colors, etc)
        /// </summary>
        private RenderSkin _selectedRenderSkin;
        public RenderSkin SelectedRenderSkin
        {
            get
            {
                if (_selectedRenderSkin == null && SkinList.Count > 0)
                {
                    _selectedRenderSkin = SkinList.Where(x => x.Name == "default_skin").SingleOrDefault();
                    if (_selectedRenderSkin == null) _selectedRenderSkin = SkinList.First();
                }
                return _selectedRenderSkin;
            }
            set
            {
                if (value != _selectedRenderSkin)
                {
                    _selectedRenderSkin = value;
                }
            }

        }

        /// <summary>
        /// constructor
        /// </summary>
        public SystemOfPersistence()
        {
            Protocol = new Protocol();

            DaphneStore = new Level("", "Config\\temp_daphnestore.json");
            UserStore = new Level("", "Config\\temp_userstore.json");

            SkinList = new ObservableCollection<RenderSkin>();

        }

        /// <summary>
        /// deserialize the daphne store
        /// </summary>
        /// <param name="tempFiles">true for handling temporary files</param>
        public void DeserializeDaphneStore(bool tempFiles = false)
        {
            DaphneStore = DaphneStore.Deserialize(tempFiles);
        }

        /// <summary>
        /// deserialize the daphne store; the latter given as a string
        /// </summary>
        /// <param name="jsonFile">json file content as string</param>
        public void DeserializeDaphneStoreFromString(string jsonFile)
        {
            DaphneStore = DaphneStore.DeserializeFromString(jsonFile);
        }

        /// <summary>
        /// deserialize the user store
        /// </summary>
        /// <param name="tempFiles">true for handling temporary files</param>
        public void DeserializeUserStore(bool tempFiles = false)
        {
            UserStore = UserStore.Deserialize(tempFiles);
        }

        /// <summary>
        /// deserialize the user store; the latter given as a string
        /// </summary>
        /// <param name="jsonFile">json file content as string</param>
        public void DeserializeUserStoreFromString(string jsonFile)
        {
            UserStore = UserStore.DeserializeFromString(jsonFile);
        }

        /// <summary>
        /// deserialize the protocol
        /// </summary>
        /// <param name="tempFiles">true for handling temporary files</param>
        public void DeserializeProtocol(bool tempFiles = false)
        {
            Protocol = (Protocol)Protocol.Deserialize(tempFiles);
        }

        /// <summary>
        /// deserialize an external protocol (not the one part of this class)
        /// </summary>
        /// <param name="tempFiles">true for handling temporary files</param>
        public static void DeserializeExternalProtocol(ref Protocol protocol, bool tempFiles = false)
        {
            protocol = (Protocol)protocol.Deserialize(tempFiles);
        }

        /// <summary>
        /// deserialize the protocol from a string
        /// </summary>
        /// <param name="jsonFile">json file content as string</param>
        public void DeserializeProtocolFromString(string jsonFile)
        {
            Protocol = (Protocol)Protocol.DeserializeFromString(jsonFile);
        }

        /// <summary>
        /// deserialize an external protocol from a string
        /// </summary>
        /// <param name="jsonFile">json file content as string</param>
        public static void DeserializeExternalProtocolFromString(ref Protocol protocol, string jsonFile)
        {
            protocol = (Protocol)protocol.DeserializeFromString(jsonFile);
        }

        public RenderCell GetRenderCell(string label)
        {
            if (SelectedRenderSkin == null)
            {
                SelectedRenderSkin = SkinList.First();
            }
            if (SelectedRenderSkin == null) return null;
            return SelectedRenderSkin.renderCells.Where(x => x.renderLabel == label).SingleOrDefault();
        }

        public RenderMol GetRenderMol(string label)
        {
            if (SelectedRenderSkin == null) SelectedRenderSkin = SkinList.First();
            if (SelectedRenderSkin == null) return null;
            return SelectedRenderSkin.renderMols.Where(x => x.renderLabel == label).SingleOrDefault();
        }


    }

    public enum PushLevel { Protocol = 0, UserStore, DaphneStore }
    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(PushLevel), typeof(string))]
    public class PushLevelToStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the BoundaryFace enum...
        private List<string> _push_level_strings = new List<string>()
                                {
                                    "Protocol",
                                    "User Store",
                                    "Daphne Store"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                int index = (int)value;
                return _push_level_strings[index];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _push_level_strings.FindIndex(item => item == str);
            return (PushLevel)Enum.ToObject(typeof(PushLevel), (int)idx);
        }
    }



    ///////////////////////////////////////////////////////////////////////////////


    /// <summary>
    /// base for all levels
    /// </summary>
    public class Level
    {
        /// <summary>
        /// constructor
        /// </summary>
        public Level()
            : this("", "")
        {
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="fileName">name of the storage file</param>
        /// <param name="tempFile">name of the temporary file</param>
        public Level(string fileName, string tempFile)
        {
            if (tempFile == null)
            {
                throw new ArgumentNullException("filename");
            }

            FileName = fileName;
            TempFile = tempFile;
            entity_repository = new EntityRepository();
        }

        // given a reaction template type, find its guid
        public string findReactionTemplateGuid(ReactionType rt)
        {
            foreach (ConfigReactionTemplate crt in entity_repository.reaction_templates)
            {
                if (crt.reac_type == rt)
                {
                    return crt.entity_guid;
                }
            }
            return "";
        }


        /// <summary>
        /// enum for push status
        /// </summary>
        public enum PushStatus { PUSH_INVALID, PUSH_CREATE_ITEM, PUSH_EXISTING_ITEM };

        /// <summary>
        /// check for existence of and whether the entity to test is newer; this applies to the entities that are editable
        /// ConfigMolecule, ConfigTransitionDriver, ConfigTransitionScheme, ConfigReaction, ConfigCell, ConfigReactionComplex
        /// </summary>
        /// <param name="e">entity to test</param>
        /// <returns>status</returns>
        public PushStatus pushStatus(ConfigEntity e)
        {
            if (e is ConfigMolecule)
            {
                // item does not exist
                if (entity_repository.molecules_dict.ContainsKey(e.entity_guid) == false)
                {
                    return PushStatus.PUSH_CREATE_ITEM;
                }

                // existing
                return PushStatus.PUSH_EXISTING_ITEM;
            }
            else if (e is ConfigGene)
            {
                // item does not exist
                if (entity_repository.genes_dict.ContainsKey(e.entity_guid) == false)
                {
                    return PushStatus.PUSH_CREATE_ITEM;
                }

                // existing
                return PushStatus.PUSH_EXISTING_ITEM;
            }
            else if (e is ConfigTransitionDriver)
            {
                // item does not exist
                if (entity_repository.transition_drivers_dict.ContainsKey(e.entity_guid) == false)
                {
                    return PushStatus.PUSH_CREATE_ITEM;
                }

                // existing
                return PushStatus.PUSH_EXISTING_ITEM;
            }
            else if (e is ConfigTransitionScheme)
            {
                // item does not exist
                if (entity_repository.diff_schemes_dict.ContainsKey(e.entity_guid) == false)
                {
                    return PushStatus.PUSH_CREATE_ITEM;
                }

                // existing
                return PushStatus.PUSH_EXISTING_ITEM;
            }
            else if (e is ConfigReaction)
            {
                // item does not exist
                if (entity_repository.reactions_dict.ContainsKey(e.entity_guid) == false)
                {
                    return PushStatus.PUSH_CREATE_ITEM;
                }

                // existing
                return PushStatus.PUSH_EXISTING_ITEM;
            }
            else if (e is ConfigReactionTemplate)
            {
                // item does not exist
                if (entity_repository.reaction_templates_dict.ContainsKey(e.entity_guid) == false)
                {
                    return PushStatus.PUSH_CREATE_ITEM;
                }

                // existing
                return PushStatus.PUSH_EXISTING_ITEM;
            }
            else if (e is ConfigCell)
            {
                // item does not exist
                if (entity_repository.cells_dict.ContainsKey(e.entity_guid) == false)
                {
                    return PushStatus.PUSH_CREATE_ITEM;
                }

                // existing
                return PushStatus.PUSH_EXISTING_ITEM;
            }
            else if (e is ConfigReactionComplex)
            {
                // item does not exist
                if (entity_repository.reaction_complexes_dict.ContainsKey(e.entity_guid) == false)
                {
                    return PushStatus.PUSH_CREATE_ITEM;
                }

                // existing
                return PushStatus.PUSH_EXISTING_ITEM;
            }

            // must be invalid type
            return PushStatus.PUSH_INVALID;
        }

        /// <summary>
        /// push a newer entity into the entity repository of this level
        /// </summary>
        /// <param name="e">entity to push</param>
        /// <param name="s">the push status of e</param>
        public void repositoryPush(ConfigEntity e, PushStatus s)
        {
            if (e is ConfigGene)
            {
                // insert
                if (s == PushStatus.PUSH_CREATE_ITEM)
                {
                    entity_repository.genes.Add(e as ConfigGene);
                    if (entity_repository.genes_dict.ContainsKey(e.entity_guid) == false)
                    {
                        entity_repository.genes_dict.Add(e.entity_guid, e as ConfigGene);
                    }
                }
                // update
                else
                {
                    // list update
                    for (int i = 0; i < entity_repository.genes.Count; i++)
                    {
                        if (entity_repository.genes[i].entity_guid == e.entity_guid)
                        {
                            entity_repository.genes[i] = e as ConfigGene;
                        }
                    }
                    // dict update
                    entity_repository.genes_dict[e.entity_guid] = e as ConfigGene;
                }
            }
            else if (e is ConfigMolecule)
            {
                // insert
                if (s == PushStatus.PUSH_CREATE_ITEM)
                {
                    entity_repository.molecules.Add(e as ConfigMolecule);
                    if (entity_repository.molecules_dict.ContainsKey(e.entity_guid) == false)
                    {
                        entity_repository.molecules_dict.Add(e.entity_guid, e as ConfigMolecule);
                    }
                }
                // update
                else
                {
                    // list update
                    for (int i = 0; i < entity_repository.molecules.Count; i++)
                    {
                        if (entity_repository.molecules[i].entity_guid == e.entity_guid)
                        {
                            entity_repository.molecules[i] = e as ConfigMolecule;
                        }
                    }
                    // dict update
                    entity_repository.molecules_dict[e.entity_guid] = e as ConfigMolecule;
                }
            }
            else if (e is ConfigTransitionDriver)
            {
                // insert
                if (s == PushStatus.PUSH_CREATE_ITEM)
                {
                    entity_repository.transition_drivers.Add(e as ConfigTransitionDriver);
                    if (entity_repository.transition_drivers_dict.ContainsKey(e.entity_guid) == false)
                    {
                        entity_repository.transition_drivers_dict.Add(e.entity_guid, e as ConfigTransitionDriver);
                    }
                }
                // update
                else
                {
                    // list update
                    for (int i = 0; i < entity_repository.transition_drivers.Count; i++)
                    {
                        if (entity_repository.transition_drivers[i].entity_guid == e.entity_guid)
                        {
                            entity_repository.transition_drivers[i] = e as ConfigTransitionDriver;
                        }
                    }
                    // dict update
                    entity_repository.transition_drivers_dict[e.entity_guid] = e as ConfigTransitionDriver;
                }
            }
            else if (e is ConfigTransitionScheme)
            {
                // insert
                if (s == PushStatus.PUSH_CREATE_ITEM)
                {
                    entity_repository.diff_schemes.Add(e as ConfigTransitionScheme);
                    if (entity_repository.diff_schemes_dict.ContainsKey(e.entity_guid) == false)
                    {
                        entity_repository.diff_schemes_dict.Add(e.entity_guid, e as ConfigTransitionScheme);
                    }
                }
                // update
                else
                {
                    // list update
                    for (int i = 0; i < entity_repository.diff_schemes.Count; i++)
                    {
                        if (entity_repository.diff_schemes[i].entity_guid == e.entity_guid)
                        {
                            entity_repository.diff_schemes[i] = e as ConfigTransitionScheme;
                        }
                    }
                    // dict update
                    entity_repository.diff_schemes_dict[e.entity_guid] = e as ConfigTransitionScheme;
                }
            }
            else if (e is ConfigReaction)
            {
                // insert
                if (s == PushStatus.PUSH_CREATE_ITEM)
                {
                    entity_repository.reactions.Add(e as ConfigReaction);
                    if (entity_repository.reactions_dict.ContainsKey(e.entity_guid) == false)
                    {
                        entity_repository.reactions_dict.Add(e.entity_guid, e as ConfigReaction);
                    }
                }
                // update
                else
                {
                    // list update
                    for (int i = 0; i < entity_repository.reactions.Count; i++)
                    {
                        if (entity_repository.reactions[i].entity_guid == e.entity_guid)
                        {
                            entity_repository.reactions[i] = e as ConfigReaction;
                        }
                    }
                    // dict update
                    entity_repository.reactions_dict[e.entity_guid] = e as ConfigReaction;
                }
            }
            else if (e is ConfigReactionTemplate)
            {
                // insert
                if (s == PushStatus.PUSH_CREATE_ITEM)
                {
                    entity_repository.reaction_templates.Add(e as ConfigReactionTemplate);
                    if (entity_repository.reaction_templates_dict.ContainsKey(e.entity_guid) == false)
                    {
                        entity_repository.reaction_templates_dict.Add(e.entity_guid, e as ConfigReactionTemplate);
                    }
                }
                // update
                else
                {
                    // list update
                    for (int i = 0; i < entity_repository.reaction_templates.Count; i++)
                    {
                        if (entity_repository.reaction_templates[i].entity_guid == e.entity_guid)
                        {
                            entity_repository.reaction_templates[i] = e as ConfigReactionTemplate;
                        }
                    }
                    // dict update
                    entity_repository.reaction_templates_dict[e.entity_guid] = e as ConfigReactionTemplate;
                }
            }
            else if (e is ConfigCell)
            {
                // insert
                if (s == PushStatus.PUSH_CREATE_ITEM)
                {
                    entity_repository.cells.Add(e as ConfigCell);
                    if (entity_repository.cells_dict.ContainsKey(e.entity_guid) == false)
                    {
                        entity_repository.cells_dict.Add(e.entity_guid, e as ConfigCell);
                    }
                }
                // update
                else
                {
                    // list update
                    for (int i = 0; i < entity_repository.cells.Count; i++)
                    {
                        if (entity_repository.cells[i].entity_guid == e.entity_guid)
                        {
                            entity_repository.cells[i] = e as ConfigCell;
                        }
                    }
                    // dict update
                    entity_repository.cells_dict[e.entity_guid] = e as ConfigCell;
                }
            }
            else if (e is ConfigReactionComplex)
            {
                // insert
                if (s == PushStatus.PUSH_CREATE_ITEM)
                {
                    entity_repository.reaction_complexes.Add(e as ConfigReactionComplex);
                    if (entity_repository.reaction_complexes_dict.ContainsKey(e.entity_guid) == false)
                    {
                        entity_repository.reaction_complexes_dict.Add(e.entity_guid, e as ConfigReactionComplex);
                    }
                }
                // update
                else
                {
                    // list update
                    for (int i = 0; i < entity_repository.reaction_complexes.Count; i++)
                    {
                        if (entity_repository.reaction_complexes[i].entity_guid == e.entity_guid)
                        {
                            entity_repository.reaction_complexes[i] = e as ConfigReactionComplex;
                        }
                    }
                    // dict update
                    entity_repository.reaction_complexes_dict[e.entity_guid] = e as ConfigReactionComplex;
                }
            }
        }

        private void InitMoleculeDict()
        {
            entity_repository.molecules_dict.Clear();
            foreach (ConfigMolecule cm in entity_repository.molecules)
            {
                entity_repository.molecules_dict.Add(cm.entity_guid, cm);
            }
            entity_repository.molecules.CollectionChanged += new NotifyCollectionChangedEventHandler(molecules_CollectionChanged);
        }

        private void InitDiffSchemeDict()
        {
            entity_repository.diff_schemes_dict.Clear();
            foreach (ConfigTransitionScheme ds in entity_repository.diff_schemes)
            {
                entity_repository.diff_schemes_dict.Add(ds.entity_guid, ds);
            }
            entity_repository.diff_schemes.CollectionChanged += new NotifyCollectionChangedEventHandler(diff_schemes_CollectionChanged);
        }

        private void InitTransitionDriversDict()
        {
            entity_repository.transition_drivers_dict.Clear();
            foreach (ConfigTransitionDriver tran in entity_repository.transition_drivers)
            {
                entity_repository.transition_drivers_dict.Add(tran.entity_guid, tran);
            }
            entity_repository.diff_schemes.CollectionChanged += new NotifyCollectionChangedEventHandler(transition_drivers_CollectionChanged);
        }

        private void InitGeneDict()
        {
            entity_repository.genes_dict.Clear();
            foreach (ConfigGene cg in entity_repository.genes)
            {
                entity_repository.genes_dict.Add(cg.entity_guid, cg);
            }
            entity_repository.genes.CollectionChanged += new NotifyCollectionChangedEventHandler(genes_CollectionChanged);
        }

        private void InitCellDict()
        {
            entity_repository.cells_dict.Clear();
            foreach (ConfigCell cc in entity_repository.cells)
            {
                entity_repository.cells_dict.Add(cc.entity_guid, cc);
            }
            entity_repository.cells.CollectionChanged += new NotifyCollectionChangedEventHandler(cells_CollectionChanged);
        }

        private void InitReactionDict()
        {
            entity_repository.reactions_dict.Clear();
            foreach (ConfigReaction cr in entity_repository.reactions)
            {
                entity_repository.reactions_dict.Add(cr.entity_guid, cr);
                cr.GetTotalReactionString(entity_repository);
            }
            entity_repository.reactions.CollectionChanged += new NotifyCollectionChangedEventHandler(reactions_CollectionChanged);
        }

        private void InitReactionComplexDict()
        {
            entity_repository.reaction_complexes_dict.Clear();
            foreach (ConfigReactionComplex crc in entity_repository.reaction_complexes)
            {
                entity_repository.reaction_complexes_dict.Add(crc.entity_guid, crc);
            }
            entity_repository.reaction_complexes.CollectionChanged += new NotifyCollectionChangedEventHandler(reaction_complexes_CollectionChanged);
        }

        private void InitReactionTemplateDict()
        {
            entity_repository.reaction_templates_dict.Clear();
            foreach (ConfigReactionTemplate crt in entity_repository.reaction_templates)
            {
                entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);
            }
            entity_repository.reaction_templates.CollectionChanged += new NotifyCollectionChangedEventHandler(template_reactions_CollectionChanged);
        }

        /// <summary>
        /// CollectionChanged not called during deserialization, so manual call to set up utility classes.
        /// Also take care of any other post-deserialization setup.
        /// </summary>
        public virtual void InitializeStorageClasses()
        {
            // GenerateNewExperimentGUID();
            InitMoleculeDict();
            InitCellDict();
            InitReactionTemplateDict();
            InitGeneDict();
            InitReactionDict();
            InitReactionComplexDict();
            InitDiffSchemeDict();
            InitTransitionDriversDict();
        }

        //genes_CollectionChanged
        private void genes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigGene cg = nn as ConfigGene;

                    if (cg != null)
                    {
                        entity_repository.genes_dict.Add(cg.entity_guid, cg);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigGene cg = dd as ConfigGene;

                    //Remove gene from genes_dict
                    entity_repository.genes_dict.Remove(cg.entity_guid);
                }
            }
        }

        //diff_schemes_CollectionChanged
        private void diff_schemes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigTransitionScheme cds = nn as ConfigTransitionScheme;

                    if (cds != null)
                    {
                        entity_repository.diff_schemes_dict.Add(cds.entity_guid, cds);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigTransitionScheme cds = dd as ConfigTransitionScheme;

                    //Remove gene from genes_dict
                    entity_repository.diff_schemes_dict.Remove(cds.entity_guid);
                }
            }
        }

        private void transition_drivers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigTransitionDriver tran = nn as ConfigTransitionDriver;

                    if (tran != null)
                    {
                        entity_repository.transition_drivers_dict.Add(tran.entity_guid, tran);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigTransitionDriver tran = dd as ConfigTransitionDriver;

                    //Remove gene from transition_drivers_dict
                    entity_repository.transition_drivers_dict.Remove(tran.entity_guid);
                }
            }
        }

        protected virtual void molecules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Need to figure out how to signal to the collection view source that the collection has changed and it should refresh
            // This is not currently a problem because it is handled in memb_molecule_combo_box_GotFocus and cyto_molecule_combo_box_GotFocus
            // But this may be the better place to handle it.

            // Raise a CollectionChanged event with Action set to Reset to refresh the UI. 
            // cvsBoundaryMolListView.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            // NotifyCollectionChangedEventArgs a = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            // entity_repository.molecules    ///OnCollectionChanged(a);


            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigMolecule cm = nn as ConfigMolecule;
                    entity_repository.molecules_dict.Add(cm.entity_guid, cm);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigMolecule cm = dd as ConfigMolecule;

                    //Remove molecule from molecules_dict
                    entity_repository.molecules_dict.Remove(cm.entity_guid);

                    //Remove all the cell membrane molpops that have this molecule type
                    foreach (ConfigCell cell in entity_repository.cells)
                    {
                        foreach (ConfigMolecularPopulation cmp in cell.membrane.molpops.ToList())
                        {
                            if (cmp.molecule.entity_guid == cm.entity_guid)
                            {
                                cell.membrane.molpops.Remove(cmp);
                            }
                        }
                    }

                    //Remove all the cell cytosol molpops that have this molecule type
                    foreach (ConfigCell cell in entity_repository.cells)
                    {
                        foreach (ConfigMolecularPopulation cmp in cell.cytosol.molpops.ToList())
                        {
                            if (cmp.molecule.entity_guid == cm.entity_guid)
                            {
                                cell.cytosol.molpops.Remove(cmp);
                            }
                        }
                    }

                    //Remove all the reactions that use this molecule
                    foreach (KeyValuePair<string, ConfigReaction> kvp in entity_repository.reactions_dict.ToList())
                    {
                        ConfigReaction reac = kvp.Value;
                        if (reac.HasMolecule(cm.entity_guid))
                        {
                            entity_repository.reactions_dict.Remove(kvp.Key);
                            entity_repository.reactions.Remove(kvp.Value);
                        }
                    }
                }
            }
        }

        protected virtual void cells_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigCell cc = nn as ConfigCell;
                    entity_repository.cells_dict.Add(cc.entity_guid, cc);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigCell cc = dd as ConfigCell;

                    //Remove this guid from ER cells_dict
                    entity_repository.cells_dict.Remove(cc.entity_guid);
                }
            }
        }

        protected virtual void reactions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigReaction cr = nn as ConfigReaction;
                    entity_repository.reactions_dict.Add(cr.entity_guid, cr);
                    cr.GetTotalReactionString(entity_repository);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReaction cr = dd as ConfigReaction;

                    // Remove entry from ER reactions_dict
                    entity_repository.reactions_dict.Remove(cr.entity_guid);

                    // Remove all the ER reaction complex reactions that have this guid
                    foreach (ConfigReactionComplex rc in entity_repository.reaction_complexes)
                    {
                        if (rc.reactions_dict.ContainsKey(cr.entity_guid) == true)
                        {
                            rc.reactions.Remove(cr);
                        }
                    }

                    // Remove all the cell membrane/cytosol reactions that have this guid
                    foreach (ConfigCell cell in entity_repository.cells)
                    {
                        if (cell.membrane.reactions_dict.ContainsKey(cr.entity_guid) == true)
                        {
                            cell.membrane.Reactions.Remove(cr);
                        }

                        if (cell.cytosol.reactions_dict.ContainsKey(cr.entity_guid) == true)
                        {
                            cell.cytosol.Reactions.Remove(cr);
                        }
                    }
                }
            }
        }

        private void template_reactions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigReactionTemplate crt = nn as ConfigReactionTemplate;
                    entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReactionTemplate crt = dd as ConfigReactionTemplate;
                    entity_repository.reaction_templates_dict.Remove(crt.entity_guid);
                }
            }
        }

        private void reaction_complexes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigReactionComplex crc = nn as ConfigReactionComplex;
                    entity_repository.reaction_complexes_dict.Add(crc.entity_guid, crc);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReactionComplex crt = dd as ConfigReactionComplex;
                    entity_repository.reaction_complexes_dict.Remove(crt.entity_guid);
                }
            }
        }

        /// <summary>
        /// New repositoryPush - This is for compound entities.  
        /// It will push the entity and its sub-entities too, into this level's entity repository.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="s"></param>
        /// <param name="sourceLevel"></param>
        /// <param name="recurse"></param>
        public void repositoryPush(ConfigEntity e, PushStatus s, Level sourceLevel, bool recurse = false)
        {
            //First push the sub-entities
            if (recurse == true)
            {
                if (e is ConfigCell)
                {
                    CellPusher(e as ConfigCell, sourceLevel, s);    //Or could add this in ConfigCell - cell = e as ConfigCell; cell.Pusher(sourceLevel, this);
                }
                else if (e is ConfigReaction)
                {
                    ReactionPusher(e as ConfigReaction, sourceLevel, s);
                }
                else if (e is ConfigTransitionScheme)
                {
                    SchemePusher(e as ConfigTransitionScheme, sourceLevel, s);
                }
                else if (e is ConfigReactionComplex)
                {
                    ReactionComplexPusher(e as ConfigReactionComplex, sourceLevel, s);
                }
            }
            else
            {
                repositoryPush(e, s);
            }
        }

        private void CellPusher(ConfigCell cell, Level sourceLevel, PushStatus s)
        {
            //Cytosol molecules
            foreach (ConfigMolecularPopulation molpop in cell.cytosol.molpops)
            {
                PushStatus s2 = pushStatus(molpop.molecule);
                if (s2 != PushStatus.PUSH_INVALID)
                {
                    ConfigMolecule newmol = molpop.molecule.Clone(null);
                    repositoryPush(newmol, s2);
                }
            }

            //Membrane molecules
            foreach (ConfigMolecularPopulation molpop in cell.membrane.molpops)
            {
                PushStatus s2 = pushStatus(molpop.molecule);
                if (s2 != PushStatus.PUSH_INVALID)
                {
                    ConfigMolecule newmol = molpop.molecule.Clone(null);
                    repositoryPush(newmol, s2);
                }
            }

            //Genes
            foreach (ConfigGene gene in cell.genes)
            {
                PushStatus s2 = pushStatus(gene);
                if (s2 != PushStatus.PUSH_INVALID)
                {
                    ConfigGene newgene = gene.Clone(null);
                    repositoryPush(newgene, s2);
                }
            }

            //Cytosol reactions
            foreach (ConfigReaction reac in cell.cytosol.Reactions)
            {
                ReactionPusher(reac, sourceLevel, s);
            }

            //Membrane reactions
            foreach (ConfigReaction reac in cell.membrane.Reactions)
            {
                ReactionPusher(reac, sourceLevel, s);
            }

            //Differentiation scheme
            SchemePusher(cell.diff_scheme, sourceLevel, s);

            //Division scheme
            SchemePusher(cell.div_scheme, sourceLevel, s);

            //Now push the cell itself
            if (s != PushStatus.PUSH_INVALID)
            {
                repositoryPush(cell, s);
            }
        }

        private void ReactionPusher(ConfigReaction reac, Level sourceLevel, PushStatus s)
        {
            //ReactionTemplate
            if (sourceLevel.entity_repository.reaction_templates_dict.ContainsKey(reac.reaction_template_guid_ref))
            {
                ReactionTemplatePusher(sourceLevel.entity_repository.reaction_templates_dict[reac.reaction_template_guid_ref]);
            }
            //else
            //{
            //    foreach (ConfigReactionTemplate crt in sourceLevel.entity_repository.reaction_templates)
            //    {
            //        if (crt.entity_guid == reac.reaction_template_guid_ref)
            //        {
            //            ReactionTemplatePusher(crt);
            //            break;
            //        }
            //    }
            //}

            //Molecules and Genes
            foreach (string guid in reac.reactants_molecule_guid_ref)
            {
                MoleculeGenePusher(guid, sourceLevel);
            }

            foreach (string guid in reac.products_molecule_guid_ref)
            {
                MoleculeGenePusher(guid, sourceLevel);
            }

            foreach (string guid in reac.modifiers_molecule_guid_ref)
            {
                MoleculeGenePusher(guid, sourceLevel);
            }

            //Now push the reaction itself
            PushStatus s2 = pushStatus(reac);
            if (s2 != PushStatus.PUSH_INVALID)
            {
                ConfigReaction newreac = reac.Clone(true);
                repositoryPush(newreac, s2);
            }
        }

        private void MoleculeGenePusher(string guid, Level sourceLevel)
        {
            ConfigEntity entity = null;

            //Figure out if guid is for molecule or gene in the source er
            if (sourceLevel.entity_repository.molecules_dict.ContainsKey(guid))
            {
                entity = sourceLevel.entity_repository.molecules_dict[guid];
            }
            else if (sourceLevel.entity_repository.genes_dict.ContainsKey(guid))
            {
                entity = sourceLevel.entity_repository.genes_dict[guid];
            }

            //foreach (ConfigMolecule mol in sourceLevel.entity_repository.molecules)
            //{
            //    if (mol.entity_guid == guid)
            //    {
            //        entity = mol;
            //        break;
            //    }
            //}

            //if (entity == null)
            //{
            //    foreach (ConfigGene gene in sourceLevel.entity_repository.genes)
            //    {
            //        if (gene.entity_guid == guid)
            //        {
            //            entity = gene;
            //            break;
            //        }
            //    }
            //}

            //Now if entity is not null, we have the entity and must push it unless it is already in target ER
            if (entity != null)
            {
                PushStatus s2 = this.pushStatus(entity);
                if (s2 != PushStatus.PUSH_INVALID)
                {
                    if (entity is ConfigGene)
                    {
                        ConfigGene newgene = ((ConfigGene)entity).Clone(null);
                        repositoryPush(newgene, s2);
                    }
                    else
                    {
                        ConfigMolecule newmol = ((ConfigMolecule)entity).Clone(null);
                        repositoryPush(newmol, s2);
                    }
                }
            }
        }

        private void ReactionComplexPusher(ConfigReactionComplex rc, Level sourceLevel, PushStatus s)
        {
            //Genes
            //foreach (ConfigGene gene in rc.genes)
            //{
            //    PushStatus s2 = pushStatus(gene);
            //    if (s2 != PushStatus.PUSH_INVALID && s2 != PushStatus.PUSH_OLDER_ITEM)
            //    {
            //        ConfigGene newgene = gene.Clone(null);
            //        repositoryPush(newgene, s2);
            //    }
            //}

            //Reactions
            foreach (ConfigReaction reac in rc.reactions)
            {
                ReactionPusher(reac, sourceLevel, s);
            }

            //Molecules
            foreach (ConfigMolecularPopulation molpop in rc.molpops)
            {
                PushStatus s2 = pushStatus(molpop.molecule);
                if (s2 != PushStatus.PUSH_INVALID)
                {
                    ConfigMolecule newmol = molpop.molecule.Clone(null);
                    repositoryPush(newmol, s2);
                }
            }

            //Push the reaction complex itself
            if (s != PushStatus.PUSH_INVALID)
            {
                repositoryPush(rc, s);
            }

        }

        private void SchemePusher(ConfigTransitionScheme scheme, Level sourceLevel, PushStatus s)
        {
            if (scheme == null)
                return;

            foreach (string guid in scheme.genes)
            {
                ConfigGene gene = FindGene(guid, sourceLevel);
                if (gene != null)
                {
                    PushStatus s2 = pushStatus(gene);
                    if (s2 != PushStatus.PUSH_INVALID)
                    {
                        ConfigGene newgene = gene.Clone(null);
                        repositoryPush(newgene, s2);
                    }
                }
            }

            //Now push the scheme itself
            PushStatus s3 = pushStatus(scheme);
            if (s3 != PushStatus.PUSH_INVALID)
            {
                ConfigTransitionScheme newscheme = scheme.Clone(true);
                repositoryPush(newscheme, s3);
            }
        }


        private void ReactionTemplatePusher(ConfigReactionTemplate crt)
        {
            PushStatus s2 = pushStatus(crt);
            if (s2 != PushStatus.PUSH_INVALID)
            {
                ConfigReactionTemplate newcrt = crt.Clone(true);
                repositoryPush(newcrt, s2);
            }
        }

        private ConfigGene FindGene(string guid, Level level)
        {
            if (level.entity_repository.genes_dict.ContainsKey(guid))
                return level.entity_repository.genes_dict[guid];

            //foreach (ConfigGene g in level.entity_repository.genes)
            //{
            //    if (g.entity_guid == guid)
            //    {
            //        return g;
            //    }
            //}

            return null;
        }



        //-------------------------------------------------------



        /// <summary>
        /// serialize the level to file
        /// </summary>
        /// <param name="tempFiles">true when wanting to serialize temporary file(s)</param>
        public void SerializeToFile(bool tempFiles = false)
        {
            //skg daphne serialize to json Thursday, April 18, 2013
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;

            //serialize Protocol
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = tempFiles == true ? TempFile : FileName;

            try
            {
                File.WriteAllText(jsonFile, jsonSpec);
            }
            catch
            {
                MessageBox.Show("File.WriteAllText failed in SerializeToFile. Filename and TempFile = " + FileName + ", " + TempFile);
            }
        }

        /// <summary>
        /// serialize to string
        /// </summary>
        /// <returns>level content as string</returns>
        public string SerializeToString()
        {
            //skg daphne serialize to json string Wednesday, May 08, 2013
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            return jsonSpec;
        }

        /// <summary>
        /// deserialize this level
        /// </summary>
        /// <param name="tempFiles">true when wanting to deserialize temporary file(s)</param>
        /// <returns>deserialized level as object for further assignment</returns>
        public virtual Level Deserialize(bool tempFiles = false)
        {
            //Deserialize JSON
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            //deserialize
            string jsonFile = tempFiles == true ? TempFile : FileName;
            string readText = File.ReadAllText(jsonFile);
            Level local = JsonConvert.DeserializeObject<Level>(readText, settings);

            Level local_copy = JsonConvert.DeserializeObject<Level>(readText, settings);

            // after deserialization the names are blank, restore them
            local.FileName = FileName;
            local.TempFile = TempFile;

            local.InitializeStorageClasses();

            return local;
        }

        /// <summary>
        /// deserialize this level from string format
        /// </summary>
        /// <param name="jsonFile">file content in string</param>
        /// <returns>deserialized level as object for further assignment</returns>
        public virtual Level DeserializeFromString(string jsonFile)
        {
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            Level local = JsonConvert.DeserializeObject<Level>(jsonFile, settings);

            // after deserialization the names are blank, restore them
            local.FileName = FileName;
            local.TempFile = TempFile;

            local.InitializeStorageClasses();

            return local;
        }

        [JsonIgnore]
        public string FileName { get; set; }
        [JsonIgnore]
        public string TempFile { get; set; }

        /// <summary>
        /// entity repository storing all available entities in this level
        /// </summary>
        public EntityRepository entity_repository { get; set; }
    }

    /// <summary>
    /// the protocol is a special type of level; it has extra information that set up an experiment with the entities of the entity repository
    /// </summary>
    public class Protocol : Level
    {
        public enum ScenarioType { UNASSIGNED, TISSUE_SCENARIO, VAT_REACTION_COMPLEX };

        public static int SafeCellPopulationID = 0;
        public string experiment_name { get; set; }
        public int experiment_reps { get; set; }
        public string experiment_guid { get; set; }
        public string experiment_description { get; set; }
        public ScenarioBase scenario { get; set; }
        public SimulationParams sim_params { get; set; }
        public string reporter_file_name { get; set; }


        /// <summary>
        /// constructor
        /// </summary>
        public Protocol()
            : this("", "", ScenarioType.UNASSIGNED)
        {
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="fileName">protocol file</param>
        /// <param name="tempFile">temporary file</param>
        public Protocol(string fileName, string tempFile, ScenarioType type)
            : base(fileName, tempFile)
        {
            Guid id = Guid.NewGuid();

            experiment_guid = id.ToString();
            experiment_name = "Experiment1";
            experiment_reps = 1;
            experiment_description = "Whole sim config description";

            if (type == ScenarioType.TISSUE_SCENARIO)
            {
                scenario = new TissueScenario();
            }
            else if (type == ScenarioType.VAT_REACTION_COMPLEX)
            {
                scenario = new VatReactionComplexScenario();
            }
            else if (type != ScenarioType.UNASSIGNED)
            {
                throw new NotImplementedException();
            }

            sim_params = new SimulationParams();

            //////LoadDefaultGlobalParameters();

            reporter_file_name = "";
        }

        /// <summary>
        /// retrieve the scenario type
        /// </summary>
        /// <returns>the type</returns>
        public ScenarioType GetScenarioType()
        {
            if (scenario == null)
            {
                return ScenarioType.UNASSIGNED;
            }
            else if (scenario is TissueScenario)
            {
                return ScenarioType.TISSUE_SCENARIO;
            }
            else if (scenario is VatReactionComplexScenario)
            {
                return ScenarioType.VAT_REACTION_COMPLEX;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// check if this scenario is of a given type
        /// </summary>
        /// <param name="type">reference type</param>
        /// <returns>true for match</returns>
        public bool CheckScenarioType(ScenarioType type)
        {
            return GetScenarioType() == type;
        }

        /// <summary>
        /// serialize the protocol to a string, skip the 'decorations', i.e. experiment name and description
        /// </summary>
        /// <returns>the protocol serialized to a string</returns>
        public string SerializeToStringSkipDeco()
        {
            // remember name and description
            string exp_name = experiment_name,
                   exp_desc = experiment_description,
                   ret;

            // temporarily set name and description to empty strings
            experiment_name = "";
            experiment_description = "";
            // serialize to string
            ret = SerializeToString();
            // reset to the remembered string values
            experiment_name = exp_name;
            experiment_description = exp_desc;
            // return serialized string
            return ret;
        }

        /// <summary>
        /// override deserialization for the protocol; needs to handle extra data only contained in the protocol level
        /// </summary>
        /// <param name="tempFiles">true to indicate deserialization of the temporary file(s)</param>
        /// <returns>deserialized protocol as Level object</returns>
        public override Level Deserialize(bool tempFiles = false)
        {
            //Deserialize JSON
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            //deserialize
            string jsonFile = tempFiles == true ? TempFile : FileName;
            string readText = File.ReadAllText(jsonFile);
            Protocol local = JsonConvert.DeserializeObject<Protocol>(readText, settings);

            // after deserialization, the names are blank, restore them
            local.FileName = FileName;
            local.TempFile = TempFile;

            local.InitializeStorageClasses();

            return local;
        }

        /// <summary>
        /// override deserialization from string for the protocol; needs to handle extra data only contained in the protocol level
        /// </summary>
        /// <param name="jsonFile">the protocol file in string format</param>
        /// <returns>deserialized protocol as Level object</returns>
        public override Level DeserializeFromString(string jsonFile)
        {
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            Protocol local = JsonConvert.DeserializeObject<Protocol>(jsonFile, settings);

            // after deserialization the names are blank, restore them
            local.FileName = FileName;
            local.TempFile = TempFile;

            local.InitializeStorageClasses();

            return local;
        }

        /// <summary>
        /// CollectionChanged not called during deserialization, so manual call to set up utility classes.
        /// Also take care of any other post-deserialization setup.
        /// </summary>
        public override void InitializeStorageClasses()
        {
            // GenerateNewExperimentGUID();
            scenario.InitializeStorageClasses();
            base.InitializeStorageClasses();
        }

        /// <summary>
        /// Any time need a new experiment GUID, such as before each run
        /// or after deserialization.
        /// </summary>
        public void GenerateNewExperimentGUID()
        {
            Guid id = Guid.NewGuid();
            experiment_guid = id.ToString();
        }

        protected override void molecules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Need to figure out how to signal to the collection view source that the collection has changed and it should refresh
            // This is not currently a problem because it is handled in memb_molecule_combo_box_GotFocus and cyto_molecule_combo_box_GotFocus
            // But this may be the better place to handle it.

            // Raise a CollectionChanged event with Action set to Reset to refresh the UI. 
            // cvsBoundaryMolListView.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            // NotifyCollectionChangedEventArgs a = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            // entity_repository.molecules    ///OnCollectionChanged(a);

            base.molecules_CollectionChanged(sender, e);

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigMolecule cm = dd as ConfigMolecule;

                    foreach (ConfigMolecularPopulation cmp in scenario.environment.comp.molpops.ToList())
                    {
                        if (cmp.molecule.entity_guid == cm.entity_guid)
                        {
                            scenario.environment.comp.molpops.Remove(cmp);
                        }
                    }
                }
            }
        }

        protected override void cells_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.cells_CollectionChanged(sender, e);

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigCell cc = dd as ConfigCell;

                    //Remove all ECM cell populations with this cell guid in the tissue scenario
                    if (scenario is TissueScenario)
                    {
                        ((TissueScenario)scenario).removeCellPopWithCellGuid(cc.entity_guid);
                    }
                }
            }
        }

        protected override void reactions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.reactions_CollectionChanged(sender, e);

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReaction cr = dd as ConfigReaction;

                    // Remove all the environment reaction complex reactions that have this guid
                    foreach (ConfigReactionComplex rc in scenario.environment.comp.reaction_complexes)
                    {
                        if (rc.reactions_dict.ContainsKey(cr.entity_guid) == true)
                        {
                            rc.reactions.Remove(cr);
                        }
                    }

                    // Remove all the ECM reactions that have this guid
                    if (scenario.environment.comp.reactions_dict.ContainsKey(cr.entity_guid) == true)
                    {
                        scenario.environment.comp.Reactions.Remove(cr);
                    }
                }
            }
        }

        public ConfigMolecule FindMolecule(string name)
        {
            ConfigMolecule gm = null;

            foreach (ConfigMolecule g in entity_repository.molecules)
            {
                if (g.Name == name)
                {
                    gm = g;
                    break;
                }
            }
            return gm;
        }

        public ConfigGene FindGene(string name)
        {
            ConfigGene cg = null;

            foreach (ConfigGene g in entity_repository.genes)
            {
                if (g.Name == name)
                {
                    cg = g;
                    break;
                }
            }
            return cg;
        }

        // given a gene name, find its guid
        public string findGeneGuid(string name, Protocol protocol)
        {
            foreach (ConfigGene gene in protocol.entity_repository.genes)
            {
                if (gene.Name == name)
                {
                    return gene.entity_guid;
                }
            }
            return "";
        }

        // given a total reaction string, find the ConfigCell object
        public bool findReactionByTotalString(string total, ObservableCollection<ConfigReaction> Reacs)
        {
            //Get left and right side molecules of new reaction
            List<string> newReactants = getReacLeftSide(total);
            List<string> newProducts = getReacRightSide(total);

            //Loop through all existing reactions
            foreach (ConfigReaction reac in Reacs)
            {
                //Get left and right side molecules of each reaction in er
                List<string> currReactants = getReacLeftSide(reac.TotalReactionString);
                List<string> currProducts = getReacRightSide(reac.TotalReactionString);

                //Key step! 
                //Check if the list of reactants and products in new reaction equals 
                //the list of reactants and products in this current reaction
                if (newReactants.SequenceEqual(currReactants) && newProducts.SequenceEqual(currProducts))
                {
                    return true;
                }
            }
            return false;
        }

        // given a total reaction string, find the ConfigCell object
        public bool findReactionByTotalString(string total, Protocol protocol)
        {
            if (findReactionByTotalString(total, protocol.entity_repository.reactions) == true)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// This method takes the ConfigReaction's TotalReactionString and returns a sorted 
        /// list of molecule strings on the left side, i.e. the reactants.
        /// </summary>
        /// <param name="total"></param>
        /// <returns></returns>
        private List<string> getReacLeftSide(string total)
        {
            int len = total.Length;
            int index = total.IndexOf("->");
            string left = total.Substring(0, index);
            left = left.Replace(" ", "");
            char[] separator = { '+' };
            string[] reactants = left.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            List<string> listLeft = new List<string>(reactants);
            listLeft.Sort();
            return listLeft;
        }

        /// <summary>
        /// This method takes the ConfigReaction's TotalReactionString and returns a sorted 
        /// list of molecule strings on the right side, i.e. the products.
        /// </summary>
        /// <param name="total"></param>
        /// <returns></returns>
        private List<string> getReacRightSide(string total)
        {
            int len = total.Length;
            int index = total.IndexOf("->");
            string right = total.Substring(index + 2);
            right = right.Replace(" ", "");
            char[] separator = { '+' };
            string[] products = right.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            List<string> listRight = new List<string>(products);
            listRight.Sort();
            return listRight;
        }


        /// <summary>
        /// Select transcription reactions in the compartment.
        /// </summary>
        /// <param name="configComp">the compartment</param>
        /// <returns></returns>
        public List<ConfigReaction> GetTranscriptionReactions(ConfigCompartment configComp)
        {
            Dictionary<string, ConfigReaction> config_reacs = new Dictionary<string, ConfigReaction>();

            // Compartment reactions
            foreach (ConfigReaction cr in configComp.Reactions)
            {
                if ((entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Transcription) && (config_reacs.ContainsKey(cr.entity_guid) == false))
                {
                    config_reacs.Add(cr.entity_guid, cr);
                }
            }

            // Compartment reaction complexes
            foreach (ConfigReactionComplex crc in configComp.reaction_complexes)
            {
                foreach (ConfigReaction cr in crc.reactions)
                {
                    if ((entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Transcription) && (config_reacs.ContainsKey(cr.entity_guid) == false))
                    {
                        config_reacs.Add(cr.entity_guid, cr);
                    }
                }
            }

            return config_reacs.Values.ToList();
        }

        /// <summary>
        /// Select boundary or bulk reactions in the compartment.
        /// Do not allow duplicate reactions.
        /// </summary>
        /// <param name="configComp">the compartment</param>
        /// 
        /// <param name="boundMol">boolean: true to select boundary, false to select bulk</param>
        /// <returns></returns>
        public List<ConfigReaction> GetReactions(ConfigCompartment configComp, bool boundMol)
        {
            Dictionary<string, ConfigReaction> config_reacs = new Dictionary<string, ConfigReaction>();

            // Compartment reactions
            foreach (ConfigReaction cr in configComp.Reactions)
            {
                if ((entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].isBoundary == boundMol) && (config_reacs.ContainsKey(cr.entity_guid) == false))
                {
                    config_reacs.Add(cr.entity_guid, cr);
                }
            }

            // Compartment reaction complexes
            foreach (ConfigReactionComplex crc in configComp.reaction_complexes)
            {
                foreach (ConfigReaction cr in crc.reactions)
                {
                    if ((entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].isBoundary == boundMol) && (config_reacs.ContainsKey(cr.entity_guid) == false))
                    {
                        config_reacs.Add(cr.entity_guid, cr);
                    }
                }
            }

            return config_reacs.Values.ToList();
        }

        public string findMoleculeGuidByName(string inputMolName)
        {
            string guid = "";
            foreach (ConfigMolecule cm in entity_repository.molecules)
            {
                if (cm.Name == inputMolName)
                {
                    guid = cm.entity_guid;
                    break;
                }
            }
            return guid;
        }

        public bool HasMoleculeType(Dictionary<string, int> inputList, MoleculeLocation molLoc)
        {
            foreach (KeyValuePair<string, int> kvp in inputList)
            {
                string guid = findMoleculeGuidByName(kvp.Key);
                if (entity_repository.molecules_dict[guid].molecule_location == molLoc)
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasGene(Dictionary<string, int> inputList)
        {
            foreach (KeyValuePair<string, int> kvp in inputList)
            {
                string guid = findGeneGuidByName(kvp.Key);
                if (guid != "")
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Given a gene name, check if it exists in repository - return guid
        /// </summary>
        /// <param name="inputGeneName"></param>
        /// <returns></returns>

        public string findGeneGuidByName(string inputGeneName)
        {
            string guid = "";
            foreach (ConfigGene cg in this.entity_repository.genes)
            {
                if (cg.Name == inputGeneName)
                {
                    guid = cg.entity_guid;
                    break;
                }
            }
            return guid;
        }

        public string IdentifyReactionType(Dictionary<string, int> inputReactants, Dictionary<string, int> inputProducts, Dictionary<string, int> inputModifiers)
        {
            string reaction_template_guid_ref = "";

            int totalReacStoich = 0;
            foreach (KeyValuePair<string, int> kvp in inputReactants)
            {
                totalReacStoich += kvp.Value;
            }

            int totalProdStoich = 0;
            foreach (KeyValuePair<string, int> kvp in inputProducts)
            {
                totalProdStoich += kvp.Value;
            }

            int totalModStoich = 0;
            foreach (KeyValuePair<string, int> kvp in inputModifiers)
            {
                totalModStoich += kvp.Value;
            }

            if (HasGene(inputReactants) || HasGene(inputProducts))
            {
                // No reactions supported for genes as reactant or product
                return reaction_template_guid_ref;
            }

            bool geneModifier = HasGene(inputModifiers);
            bool boundProd = HasMoleculeType(inputProducts, MoleculeLocation.Boundary);

            if (geneModifier)
            {
                if ((inputModifiers.Count > 1) || (inputProducts.Count != 1) || (inputReactants.Count != 0) || (totalModStoich > 1) || (totalProdStoich > 1) || (boundProd))
                {
                    // Gene transcription reaction does not support these possibilities
                    return reaction_template_guid_ref;
                }
                else
                {
                    return findReactionTemplateGuid(ReactionType.Transcription);
                }
            }


            bool bulkProd = HasMoleculeType(inputProducts, MoleculeLocation.Bulk);
            bool boundReac = HasMoleculeType(inputReactants, MoleculeLocation.Boundary);
            bool bulkReac = HasMoleculeType(inputReactants, MoleculeLocation.Bulk);
            bool boundMod = HasMoleculeType(inputModifiers, MoleculeLocation.Boundary);
            bool bulkMod = HasMoleculeType(inputModifiers, MoleculeLocation.Bulk);

            int bulkBoundVal = 1,
                    modVal = 10,
                    reacVal = 100,
                    prodVal = 1000,
                    reacStoichVal = 10000,
                    prodStoichVal = 100000,
                    modStoichVal = 1000000;

            if (inputModifiers.Count > 9 || inputReactants.Count > 9 || inputProducts.Count > 9 || totalReacStoich > 9 || totalProdStoich > 9 || totalModStoich > 9)
            {
                throw new Exception("Unsupported reaction with current typing algorithm.\n");
            }

            int reacNum = inputModifiers.Count * modVal
                            + inputReactants.Count * reacVal
                            + inputProducts.Count * prodVal
                            + totalReacStoich * reacStoichVal
                            + totalProdStoich * prodStoichVal
                            + totalModStoich * modStoichVal;

            if ((boundReac || boundProd || boundMod) && (bulkReac || bulkProd || bulkMod))
            {
                reacNum += bulkBoundVal;
            }

            switch (reacNum)
            {
                // Interior
                case 10100:
                    return findReactionTemplateGuid(ReactionType.Annihilation);
                case 121200:
                    return findReactionTemplateGuid(ReactionType.Association);
                case 121100:
                    return findReactionTemplateGuid(ReactionType.Dimerization);
                case 211100:
                    return findReactionTemplateGuid(ReactionType.DimerDissociation);
                case 212100:
                    return findReactionTemplateGuid(ReactionType.Dissociation);
                case 111100:
                    return findReactionTemplateGuid(ReactionType.Transformation);
                case 221200:
                    return findReactionTemplateGuid(ReactionType.AutocatalyticTransformation);
                // Interior Catalyzed (catalyst stoichiometry doesn't change)
                case 1010110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedAnnihilation);
                case 1121210:
                    return findReactionTemplateGuid(ReactionType.CatalyzedAssociation);
                case 1101010:
                    return findReactionTemplateGuid(ReactionType.CatalyzedCreation);
                case 1121110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedDimerization);
                case 1211110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedDimerDissociation);
                case 1212110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedDissociation);
                case 1111110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedTransformation);
                // Bulk/Boundary reactions
                case 121201:
                    if ((boundProd) && (boundReac))
                    {
                        // The product and one of the reactants must be boundary molecules 
                        return findReactionTemplateGuid(ReactionType.BoundaryAssociation);
                    }
                    else
                    {
                        return reaction_template_guid_ref;
                    }
                case 212101:
                    if ((boundProd) && (boundReac))
                    {
                        // The reactant and one of the products must be boundary molecules 
                        return findReactionTemplateGuid(ReactionType.BoundaryDissociation);
                    }
                    else
                    {
                        return reaction_template_guid_ref;
                    }
                case 111101:
                    if (boundReac)
                    {
                        return findReactionTemplateGuid(ReactionType.BoundaryTransportFrom);
                    }
                    else
                    {
                        return findReactionTemplateGuid(ReactionType.BoundaryTransportTo);
                    }
                // Catalyzed Bulk/Boundary reactions
                case 1111111:
                    if (boundMod)
                    {
                        return findReactionTemplateGuid(ReactionType.CatalyzedBoundaryActivation);
                    }
                    else
                    {
                        return reaction_template_guid_ref;
                    }
                // Generalized reaction
                default:
                    // Not implemented yet
                    return reaction_template_guid_ref;
            }
        }
    }

    // start at > 0 as zero seems to be the default for metadata when a property is not present
    public enum SimStates { Linear = 1, Cubic, Tiny, Large };

    public abstract class ScenarioBase
    {
        public ScenarioBase()
        {
            time_config = new TimeConfig();
        }

        /// <summary>
        /// special case, push into the entity level; updates all occurrences of e
        /// the base must push into the environment as all scenarios have one
        /// </summary>
        /// <param name="e">the entity to push</param>
        public virtual void entityPush(ConfigEntity e)
        {
            if (e is ConfigMolecule)
            {
                // molecules exist in compartments
                // env
                environment.comp.pushMolecule(e as ConfigMolecule);

                foreach (ConfigReactionComplex rc in environment.comp.reaction_complexes)
                {
                    rc.pushMolecule(e as ConfigMolecule);
                }
            }
            else if (e is ConfigTransitionDriver)
            {
            }
            else if (e is ConfigTransitionScheme)
            {
            }
            else if (e is ConfigReaction)
            {
                // reactions exist in compartments
                // env
                environment.comp.pushReaction(e as ConfigReaction);

                // and in reaction complexes
                foreach (ConfigReactionComplex rc in environment.comp.reaction_complexes)
                {
                    rc.pushReaction(e as ConfigReaction);
                }
            }
            else if (e is ConfigCell)
            {
            }
            else if (e is ConfigReactionComplex)
            {
                // reaction complexes exist in compartments
                // env
                environment.comp.pushReactionComplex(e as ConfigReactionComplex);
            }
        }

        /// <summary>
        /// generic function to retrieve if a given cell is present
        /// </summary>
        /// <param name="cell">the cell to test for</param>
        /// <returns></returns>
        public virtual bool HasCell(ConfigCell cell)
        {
            return false;
        }

        /// <summary>
        /// reset the Gaussian retrieve counter
        /// </summary>
        public virtual void resetGaussRetrieve()
        {
            environment.comp.resetGaussRetrieve();
        }

        /// <summary>
        /// get the next Gaussian spec
        /// </summary>
        /// <returns></returns>
        public virtual GaussianSpecification nextGaussSpec()
        {
            return environment.comp.nextGaussSpec();
        }

        /// <summary>
        /// need to override this in scenario types that have something specific to initialize
        /// </summary>
        public abstract void InitializeStorageClasses();

        public TimeConfig time_config { get; set; }
        public SimStates simInterpolate { get; set; }
        public SimStates simCellSize { get; set; }
        public ConfigEnvironmentBase environment { get; set; }
    }

    public class VatReactionComplexScenario : ScenarioBase
    {
        [JsonIgnore]
        public ObservableCollection<ConfigMolecularPopulation> AllMols { get; set; }
        [JsonIgnore]
        public ObservableCollection<ConfigReaction> AllReacs { get; set; }

        public RenderPopOptions popOptions { get; set; }
        
        public VatReactionComplexScenario()
        {
            environment = new ConfigPointEnvironment();
            AllMols = new ObservableCollection<ConfigMolecularPopulation>();
            AllReacs = new ObservableCollection<ConfigReaction>();
            environment.comp.reaction_complexes.CollectionChanged += new NotifyCollectionChangedEventHandler(reaction_complexes_CollectionChanged);

            popOptions = new RenderPopOptions();
        }

        public override void InitializeStorageClasses()
        {
            InitializeAllMols();
            InitializeAllReacs();
        }

        private void reaction_complexes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InitializeAllMols();
            InitializeAllReacs();
        }

        /// <summary>
        /// Returns true if any reaction complex in compartment contains mol pop with given renderLabel
        /// </summary>
        /// <param name="renderLabel"></param>
        /// <returns></returns>
        private bool FindMolPop(string renderLabel)
        {
            ConfigCompartment comp = environment.comp;
            foreach (ConfigReactionComplex crc in comp.reaction_complexes)
            {
                foreach (ConfigMolecularPopulation molpop in crc.molpops)
                {
                    if (molpop.renderLabel == renderLabel)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Removes from molPopOptions list after AllMols is changed.
        /// </summary>
        private void RemoveOldMolPopOptions()
        {
            foreach (RenderPop pop in popOptions.molPopOptions.ToList())
            {
                if (FindMolPop(pop.renderLabel) == false)
                {
                    popOptions.molPopOptions.Remove(pop);
                }
            }
        }

        /// <summary>
        /// Initializes AllMols after user added/removed a reaction to/from reaction complex
        /// </summary>
        public void InitializeAllMols()
        {
            AllMols.Clear();

            foreach (ConfigReactionComplex crc in environment.comp.reaction_complexes)
            {
                foreach (ConfigMolecularPopulation molpop in crc.molpops)
                {
                    bool molecule_found = AllMols.Where(m => m.molecule.entity_guid == molpop.molecule.entity_guid).Any();
                    if (molecule_found == false)
                    {
                        AllMols.Add(molpop);
                        popOptions.AddRenderOptions(molpop.renderLabel, molpop.Name, false);
                        RenderPop rp = popOptions.GetMolRenderPop(molpop.renderLabel);
                        rp.renderOn = true;
                    }
                }
            }
            RemoveOldMolPopOptions();
        }

        public void InitializeAllReacs()
        {
            AllReacs.Clear();

            foreach (ConfigReactionComplex crc in environment.comp.reaction_complexes)
            {
                foreach (ConfigReaction reac in crc.reactions)
                {
                    if (AllReacs.Contains(reac) == false)
                    {
                        AllReacs.Add(reac);
                    }
                }
            }
        }
    }

    public class TissueScenario : ScenarioBase
    {
        public ObservableCollection<CellPopulation> cellpopulations { get; set; }

        // Convenience utility storage (not serialized)
        [JsonIgnore]
        public Dictionary<int, CellPopulation> cellpopulation_id_cellpopulation_dict;

        private int gaussRetrieve, originCounter;


        public string RenderSkinName { get; set; }

        public RenderPopOptions popOptions { get; set; }

        public TissueScenario()
        {
            simInterpolate = SimStates.Linear;
            simCellSize = SimStates.Tiny;
            environment = new ConfigECSEnvironment();
            cellpopulations = new ObservableCollection<CellPopulation>();

            // Utility storage
            // NOTE: No use adding CollectionChanged event handlers here since it gets wiped out by deserialization anyway...
            cellpopulation_id_cellpopulation_dict = new Dictionary<int, CellPopulation>();
            // Set callback to update box specification extents when environment extents change
            environment.PropertyChanged += new PropertyChangedEventHandler(environment_PropertyChanged);
            RenderSkinName = "default";
            popOptions = new RenderPopOptions();
        }

        /// <summary>
        /// override this function to handle the base version and for the tissue scenario
        /// </summary>
        public override void resetGaussRetrieve()
        {
            base.resetGaussRetrieve();
            gaussRetrieve = 0;
            originCounter = 0;
        }

        /// <summary>
        /// retrieve the next Gaussian spec
        /// </summary>
        /// <returns>the Gaussian found or null when done</returns>
        public override GaussianSpecification nextGaussSpec()
        {
            GaussianSpecification next;

            if (originCounter == 0)
            {
                next = base.nextGaussSpec();
                if (next != null)
                {
                    return next;
                }
                originCounter++;
            }
            if (originCounter == 1)
            {
                while (gaussRetrieve < cellpopulations.Count && cellpopulations[gaussRetrieve].cellPopDist is CellPopGaussian == false)
                {
                    gaussRetrieve++;
                }
                // it's a Gaussian
                if (gaussRetrieve < cellpopulations.Count)
                {
                    next = ((CellPopGaussian)cellpopulations[gaussRetrieve].cellPopDist).gauss_spec;
                    gaussRetrieve++;
                    return next;
                }
                originCounter++;
            }
            return null;
        }

        private void cellsets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    CellPopulation cs = nn as CellPopulation;

                    foreach (ConfigMolecularPopulation mp in environment.comp.molpops)
                    {
                        ReportECM er = new ReportECM();

                        er.molpop_guid_ref = mp.molpop_guid;
                        cs.ecm_probe.Add(er);
                        cs.ecm_probe_dict.Add(mp.molpop_guid, er);
                    }
                    cellpopulation_id_cellpopulation_dict.Add(cs.cellpopulation_id, cs);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    CellPopulation cs = dd as CellPopulation;

                    cellpopulation_id_cellpopulation_dict.Remove(cs.cellpopulation_id);
                }
            }
        }

        public void InitCellPopulationIDCellPopulationDict()
        {
            cellpopulation_id_cellpopulation_dict.Clear();
            foreach (CellPopulation cs in cellpopulations)
            {
                cellpopulation_id_cellpopulation_dict.Add(cs.cellpopulation_id, cs);

                if (cs.cellPopDist != null)
                {
                    cs.cellPopDist.cellPop = cs;
                }
            }
            cellpopulations.CollectionChanged += new NotifyCollectionChangedEventHandler(cellsets_CollectionChanged);
        }

        public void InitGaussCellPopulationUpdates()
        {
            foreach (CellPopulation cs in cellpopulations)
            {
                if (cs.cellPopDist.DistType == CellPopDistributionType.Gaussian)
                {
                    BoxSpecification box = ((CellPopGaussian)cs.cellPopDist).gauss_spec.box_spec;
                    ((CellPopGaussian)cs.cellPopDist).ParamReset(box);
                    box.PropertyChanged += new PropertyChangedEventHandler(((CellPopGaussian)cs.cellPopDist).CellPopGaussChanged);
                }
            }
        }

        public CellPopulation GetCellPopulation(int key)
        {
            if (cellpopulation_id_cellpopulation_dict.ContainsKey(key) == true)
            {
                return cellpopulation_id_cellpopulation_dict[key];
            }
            else
            {
                throw new Exception("Population ID does not exist.");
            }
        }

        public override bool HasCell(ConfigCell cell)
        {
            bool res = false;
            foreach (CellPopulation cell_pop in cellpopulations)
            {
                if (cell_pop.Cell.entity_guid == cell.entity_guid)
                {
                    return true;
                }
            }
            return res;
        }

        public int RenderPopReferenceCount(string lable, bool IsCell)
        {
            if (IsCell == true)
            {
                return cellpopulations.Where(x => x.renderLabel == lable).Count();
            }
            else
            {
                //this need work
                throw (new NotImplementedException("Working in progress: RenderPopReferenceCount"));
                //return environment.ecs.molecules_dict.ContainsKey(lable) ? 1 : 0;
            }
        }

        /// <summary>
        /// special case, push into the entity level; updates all occurrences of e
        /// </summary>
        /// <param name="e">the entity to push</param>
        public override void entityPush(ConfigEntity e)
        {
            // call base
            base.entityPush(e);

            if (e is ConfigMolecule)
            {
                // molecules exist in compartments
                // cells
                foreach (CellPopulation cp in cellpopulations)
                {
                    cp.Cell.cytosol.pushMolecule(e as ConfigMolecule);
                    cp.Cell.membrane.pushMolecule(e as ConfigMolecule);

                    // molecules exist in reaction complexes and need to be updated there too
                    foreach (ConfigReactionComplex rc in cp.Cell.cytosol.reaction_complexes)
                    {
                        rc.pushMolecule(e as ConfigMolecule);
                    }
                    foreach (ConfigReactionComplex rc in cp.Cell.membrane.reaction_complexes)
                    {
                        rc.pushMolecule(e as ConfigMolecule);
                    }
                }

            }
            else if (e is ConfigTransitionDriver)
            {
                foreach (CellPopulation cp in cellpopulations)
                {
                    // death
                    if (cp.Cell.death_driver.entity_guid == e.entity_guid)
                    {
                        cp.Cell.death_driver = e as ConfigTransitionDriver;
                    }
                    // div

                    throw (new NotImplementedException("need work here, sanjeev"));
                    //if (cp.Cell.div_driver.entity_guid == e.entity_guid)
                    //{
                    //    if (forced == true || cp.Cell.div_driver.change_stamp < e.change_stamp)
                    //    {
                    //        cp.Cell.div_driver = e as ConfigTransitionDriver;
                    //    }
                    //}
                }
            }
            else if (e is ConfigTransitionScheme)
            {
                foreach (CellPopulation cp in cellpopulations)
                {
                    if (cp.Cell.diff_scheme.entity_guid == e.entity_guid)
                    {
                        cp.Cell.diff_scheme = e as ConfigTransitionScheme;
                    }
                }
            }
            else if (e is ConfigReaction)
            {
                // reactions exist in compartments
                // cells
                foreach (CellPopulation cp in cellpopulations)
                {
                    cp.Cell.cytosol.pushReaction(e as ConfigReaction);
                    cp.Cell.membrane.pushReaction(e as ConfigReaction);
                    // reactions exist in reaction complexes and need to be updated there too
                    foreach (ConfigReactionComplex rc in cp.Cell.cytosol.reaction_complexes)
                    {
                        rc.pushReaction(e as ConfigReaction);
                    }
                    foreach (ConfigReactionComplex rc in cp.Cell.membrane.reaction_complexes)
                    {
                        rc.pushReaction(e as ConfigReaction);
                    }
                }

            }
            else if (e is ConfigCell)
            {
                foreach (CellPopulation cp in cellpopulations)
                {
                    if (cp.Cell.entity_guid == e.entity_guid)
                    {
                        cp.Cell = e as ConfigCell;
                    }
                }
            }
            else if (e is ConfigReactionComplex)
            {
                // reaction complexes exist in compartments
                // cells
                foreach (CellPopulation cp in cellpopulations)
                {
                    cp.Cell.cytosol.pushReactionComplex(e as ConfigReactionComplex);
                    cp.Cell.membrane.pushReactionComplex(e as ConfigReactionComplex);
                }
            }
        }

        /// <summary>
        /// Routine called when the environment extent changes
        /// Updates all box specifications in repository with correct max & min for sliders in GUI
        /// Also updates VTK visual environment box
        /// Also updates cell coordinates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void environment_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Check that cells are still inside the simulation space.
            foreach (CellPopulation cellPop in cellpopulations)
            {
                cellPop.cellPopDist.Resize(new double[3] { ((ConfigECSEnvironment)environment).extent_x,
                                                           ((ConfigECSEnvironment)environment).extent_y,
                                                           ((ConfigECSEnvironment)environment).extent_z });
            }
#if USE_BOX_LIMITS
            // update all box min/max translation and scale
            foreach (BoxSpecification box in box_guid_box_dict.Values)
            {
                box.SetBoxSpecExtents((ConfigECSEnvironment)environment);
            }
#endif
        }

        /// <summary>
        /// Making sure that SafeCellPopulationID is greater than largest ID read in after deserialization.
        /// </summary>
        public void FindNextSafeCellPopulationID()
        {
            int max_id = 0;
            foreach (CellPopulation cs in cellpopulations)
            {
                if (cs.cellpopulation_id > max_id)
                    max_id = cs.cellpopulation_id;
            }
            Protocol.SafeCellPopulationID = max_id + 1;
        }

        private void ecm_molpops_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigMolecularPopulation mp = nn as ConfigMolecularPopulation;

                    ////// add molpop into molpops_dict
                    ////if (!scenario.environment.ecs.molpops_dict.ContainsKey(mp.molpop_guid))
                    ////{
                    ////    scenario.environment.ecs.molpops_dict.Add(mp.molpop_guid, mp);
                    ////}

                    // add ecm report
                    foreach (CellPopulation cp in cellpopulations)
                    {
                        ReportECM er = new ReportECM();

                        er.molpop_guid_ref = mp.molpop_guid;
                        cp.ecm_probe.Add(er);
                        cp.ecm_probe_dict.Add(mp.molpop_guid, er);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigMolecularPopulation mp = dd as ConfigMolecularPopulation;

                    ////// remove from molpops_dict
                    ////if (scenario.environment.ecs.molpops_dict.ContainsKey(mp.molpop_guid))
                    ////{
                    ////    scenario.environment.ecs.molpops_dict.Remove(mp.molpop_guid);
                    ////}

                    // remove ecm report
                    foreach (CellPopulation cp in cellpopulations)
                    {
                        // need to keep an eye on this; this poses an inefficient way of doing the removal; it should not happen excessively; if it did, we'd need a change here
                        cp.ecm_probe.Remove(cp.ecm_probe_dict[mp.molpop_guid]);
                        cp.ecm_probe_dict.Remove(mp.molpop_guid);
                    }
                }
            }
        }

        private void InitECMProbeDict()
        {
            // build ecm_probe_dict
            foreach (CellPopulation cp in cellpopulations)
            {
                cp.ecm_probe_dict.Clear();
                foreach (ReportECM recm in cp.ecm_probe)
                {
                    if (cp.ecm_probe_dict.ContainsKey(recm.molpop_guid_ref) == false)
                        cp.ecm_probe_dict.Add(recm.molpop_guid_ref, recm);
                }
            }

            environment.comp.molpops.CollectionChanged += new NotifyCollectionChangedEventHandler(ecm_molpops_CollectionChanged);
        }

        public override void InitializeStorageClasses()
        {
            FindNextSafeCellPopulationID();
            InitCellPopulationIDCellPopulationDict();
            InitGaussCellPopulationUpdates();
            InitECMProbeDict();
        }

        /// <summary>
        /// remove all cell pops with cells that have the provided guid
        /// </summary>
        /// <param name="guid">the guid</param>
        public void removeCellPopWithCellGuid(string guid)
        {
            foreach (var cell_pop in cellpopulations.ToList())
            {
                if (guid == cell_pop.Cell.entity_guid)
                    cellpopulations.Remove(cell_pop);
            }
        }

        public bool HasMoleculeInSomeCellMembrane(string mol_guid)
        {
            foreach (CellPopulation pop in cellpopulations)
            {
                if (pop.Cell.membrane.molecules_dict.ContainsKey(mol_guid))
                    return true;
            }

            return false;
        }
    }

    public class SimulationParams : EntityModelBase
    {
        public SimulationParams()
        {
            // default value
            phi1 = 100;
            deathConstant = 1e-3;
            deathOrder = 1;
            globalRandomSeed = RandomSeed.Robust();
        }
        public double phi1 { get; set; }
        public double phi2 { get; set; }
        public double deathConstant { get; set; }
        public int deathOrder { get; set; }

        private int randomSeed;
        public int globalRandomSeed
        {
            get
            {
                return randomSeed;
            }

            set
            {
                randomSeed = value;
                OnPropertyChanged("globalRandomSeed");
            }
        }
    }

    public class EntityRepository
    {
        // All molecules, reactions, cells - Combined Predefined and User defined
        public ObservableCollection<ConfigReactionComplex> reaction_complexes { get; set; }
        public ObservableCollection<ConfigCell> cells { get; set; }
        public ObservableCollection<ConfigMolecule> molecules { get; set; }
        public ObservableCollection<ConfigGene> genes { get; set; }
        public ObservableCollection<ConfigReaction> reactions { get; set; }
        public ObservableCollection<ConfigReactionTemplate> reaction_templates { get; set; }
        public ObservableCollection<ConfigTransitionScheme> diff_schemes { get; set; }
        public ObservableCollection<ConfigTransitionDriver> transition_drivers { get; set; }

        [JsonIgnore]
        public Dictionary<string, ConfigMolecule> molecules_dict; // keyed by molecule_guid
        [JsonIgnore]
        public Dictionary<string, ConfigGene> genes_dict; // keyed by gene_guid
        [JsonIgnore]
        public Dictionary<string, ConfigReactionTemplate> reaction_templates_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigReaction> reactions_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigCell> cells_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigReactionComplex> reaction_complexes_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigTransitionScheme> diff_schemes_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigTransitionDriver> transition_drivers_dict;


        public EntityRepository()
        {
            cells = new ObservableCollection<ConfigCell>();
            cells_dict = new Dictionary<string, ConfigCell>();
            molecules = new ObservableCollection<ConfigMolecule>();
            molecules_dict = new Dictionary<string, ConfigMolecule>();
            genes = new ObservableCollection<ConfigGene>();
            genes_dict = new Dictionary<string, ConfigGene>();
            reactions = new ObservableCollection<ConfigReaction>();
            reactions_dict = new Dictionary<string, ConfigReaction>();
            reaction_templates = new ObservableCollection<ConfigReactionTemplate>();
            reaction_templates_dict = new Dictionary<string, ConfigReactionTemplate>();
            reaction_complexes = new ObservableCollection<ConfigReactionComplex>();
            reaction_complexes_dict = new Dictionary<string, ConfigReactionComplex>();
            diff_schemes = new ObservableCollection<ConfigTransitionScheme>();
            diff_schemes_dict = new Dictionary<string, ConfigTransitionScheme>();
            transition_drivers = new ObservableCollection<ConfigTransitionDriver>();
            transition_drivers_dict = new Dictionary<string, ConfigTransitionDriver>();
        }
    }

    public class TimeConfig
    {
        public double duration { get; set; }
        public double rendering_interval { get; set; }
        public double sampling_interval { get; set; }
        public double integrator_step { get; set; }

        public TimeConfig()
        {
            duration = 100;
            rendering_interval = 1;
            sampling_interval = 1;
            integrator_step = 0.001;
        }
    }

    public class ConfigEnvironmentBase : EntityModelBase
    {
        public ConfigEnvironmentBase()
        {
            comp = new ConfigCompartment();
        }

        public ConfigCompartment comp { get; set; }
    }

    public class ConfigPointEnvironment : ConfigEnvironmentBase
    {
        // extents, gridstep, numGridPts, toroidal do not apply
        // no initialization required?

        public ConfigPointEnvironment()
        {
            // Nothing to do here, I think.
        }
    }

    public class ConfigRectEnvironment : ConfigEnvironmentBase
    {
        private int _extent_x;
        private int _extent_y;
        private double _gridstep;

        public int extent_x
        {
            get { return _extent_x; }
            set
            {
                if (_extent_x == value)
                    return;
                else
                {
                    int saveValue = _extent_x;
                    _extent_x = value;
                    if (CalculateNumGridPts())
                    {
                        OnPropertyChanged("extent_x");
                    }
                    else
                    {
                        _extent_x = saveValue;
                        System.Windows.MessageBox.Show("System must have at least 3 grid points on a side.");
                    }
                }
            }
        }
        public int extent_y
        {
            get { return _extent_y; }
            set
            {
                if (_extent_y == value)
                    return;
                else
                {
                    int saveValue = _extent_y;
                    _extent_y = value;
                    if (CalculateNumGridPts())
                    {
                        OnPropertyChanged("extent_y");
                    }
                    else
                    {
                        _extent_y = saveValue;
                        System.Windows.MessageBox.Show("System must have at least 3 grid points on a side.");
                    }
                }
            }
        }
        public double gridstep
        {
            get { return _gridstep; }
            set
            {
                if (_gridstep == value)
                    return;
                else
                {
                    double saveValue = _gridstep;
                    _gridstep = value;
                    if (CalculateNumGridPts())
                    {
                        OnPropertyChanged("gridstep");
                    }
                    else
                    {
                        _gridstep = saveValue;
                        System.Windows.MessageBox.Show("System must have at least 3 grid points on a side.");
                    }
                }
            }
        }
        public int[] NumGridPts { get; set; }

        [JsonIgnore]
        public int extent_min { get; set; }
        [JsonIgnore]
        public int extent_max { get; set; }
        [JsonIgnore]
        public int gridstep_min { get; set; }
        [JsonIgnore]
        public int gridstep_max { get; set; }

        private bool _toroidal;
        public bool toroidal
        {
            get { return _toroidal; }
            set
            {
                if (_toroidal == value)
                    return;
                else
                {
                    _toroidal = value;
                    OnPropertyChanged("toroidal");
                }
            }
        }

        public ConfigRectEnvironment()
        {
            gridstep = 10;
            extent_x = 200;
            extent_y = 200;
            extent_min = 5;
            extent_max = 1000;
            gridstep_min = 1;
            gridstep_max = 100;
            initialized = true;
            toroidal = false;

            // Don't need to check the boolean returned, since we know these values are okay.
            CalculateNumGridPts();
        }

        private bool initialized = false;

        private bool CalculateNumGridPts()
        {
            if (initialized == false)
            {
                return true;
            }

            int[] pt = new int[2];

            pt[0] = (int)Math.Ceiling((decimal)(extent_x / gridstep)) + 1;
            pt[1] = (int)Math.Ceiling((decimal)(extent_y / gridstep)) + 1;

            // Must have at least 3 grid points for gradient routines at boundary points
            if ((pt[0] < 3) || (pt[1] < 3))
            {
                return false;
            }

            NumGridPts = pt;

            return true;
        }
    }

    public class ConfigECSEnvironment : ConfigEnvironmentBase
    {
        private int _extent_x;
        private int _extent_y;
        private int _extent_z;
        private double _gridstep;
        public int extent_x
        {
            get { return _extent_x; }
            set
            {
                if (_extent_x == value)
                    return;
                else
                {
                    int saveValue = _extent_x;
                    _extent_x = value;
                    if (CalculateNumGridPts())
                    {
                        OnPropertyChanged("extent_x");
                    }
                    else
                    {
                        _extent_x = saveValue;
                        System.Windows.MessageBox.Show("System must have at least 3 grid points on a side.");
                    }
                }
            }
        }
        public int extent_y
        {
            get { return _extent_y; }
            set
            {
                if (_extent_y == value)
                    return;
                else
                {
                    int saveValue = _extent_y;
                    _extent_y = value;
                    if (CalculateNumGridPts())
                    {
                        OnPropertyChanged("extent_y");
                    }
                    else
                    {
                        _extent_y = saveValue;
                        System.Windows.MessageBox.Show("System must have at least 3 grid points on a side.");
                    }
                }
            }
        }
        public int extent_z
        {
            get { return _extent_z; }
            set
            {
                if (_extent_z == value)
                    return;
                else
                {
                    int saveValue = _extent_z;
                    _extent_z = value;
                    if (CalculateNumGridPts())
                    {
                        OnPropertyChanged("extent_z");
                    }
                    else
                    {
                        _extent_z = saveValue;
                        System.Windows.MessageBox.Show("System must have at least 3 grid points on a side.");
                    }
                }
            }
        }
        public double gridstep
        {
            get { return _gridstep; }
            set
            {
                if (_gridstep == value)
                    return;
                else
                {
                    double saveValue = _gridstep;
                    _gridstep = value;
                    if (CalculateNumGridPts())
                    {
                        OnPropertyChanged("gridstep");
                    }
                    else
                    {
                        _gridstep = saveValue;
                        System.Windows.MessageBox.Show("System must have at least 3 grid points on a side.");
                    }
                }
            }
        }
        public int[] NumGridPts { get; set; }

        [JsonIgnore]
        public int extent_min { get; set; }
        [JsonIgnore]
        public int extent_max { get; set; }
        [JsonIgnore]
        public int gridstep_min { get; set; }
        [JsonIgnore]
        public int gridstep_max { get; set; }

        private bool _toroidal;
        public bool toroidal
        {
            get { return _toroidal; }
            set
            {
                if (_toroidal == value)
                    return;
                else
                {
                    _toroidal = value;
                    OnPropertyChanged("toroidal");
                }
            }
        }

        public ConfigECSEnvironment()
        {
            gridstep = 10;
            extent_x = 200;
            extent_y = 200;
            extent_z = 200;
            extent_min = 5;
            extent_max = 1000;
            gridstep_min = 1;
            gridstep_max = 100;
            initialized = true;
            toroidal = false;

            // Don't need to check the boolean returned, since we know these values are okay.
            CalculateNumGridPts();
        }

        private bool initialized = false;

        private bool CalculateNumGridPts()
        {
            if (initialized == false)
            {
                return true;
            }

            int[] pt = new int[3];

            pt[0] = (int)Math.Ceiling((decimal)(extent_x / gridstep)) + 1;
            pt[1] = (int)Math.Ceiling((decimal)(extent_y / gridstep)) + 1;
            pt[2] = (int)Math.Ceiling((decimal)(extent_z / gridstep)) + 1;

            // Must have at least 3 grid points for gradient routines at boundary points
            if ((pt[0] < 3) || (pt[1] < 3) || (pt[2] < 3))
            {
                return false;
            }

            NumGridPts = pt;

            return true;
        }

        /// <summary>
        /// Check for a valid ECS reaction. 
        /// All bulk molecules must be present in the ECS.
        /// All boundary molecules must be present, as a whole group, in the membrane of at least one cell type.
        /// There must be at least one bulk molecule.
        /// </summary>
        /// <param name="cr"></param>
        /// <param name="scenario"></param>
        /// <returns></returns>
        public bool ValidateReaction(ConfigReaction cr, Protocol protocol)
        {
            bool bBulkOK = false;
            bool bBoundOK = false;

            ObservableCollection<string> boundMols = new ObservableCollection<string>();
            boundMols = cr.GetBoundaryMolecules(protocol.entity_repository);
            if (boundMols.Count == 0)
            {
                bBoundOK = true;
            }
            else
            {
                foreach (CellPopulation cellpop in ((TissueScenario)protocol.scenario).cellpopulations)
                {
                    if (cellpop.Cell.membrane.HasMolecules(boundMols) == true)
                    {
                        bBoundOK = true;
                        break;
                    }
                }
            }

            ObservableCollection<string> bulkMols = new ObservableCollection<string>();
            bulkMols = cr.GetBulkMolecules(protocol.entity_repository);
            if (bulkMols.Count > 0)
            {
                if (comp.HasMolecules(bulkMols) == true)
                {
                    bBulkOK = true;
                }
            }
            else
            {
                bBulkOK = false;
            }

            return (bBoundOK & bBulkOK);
        }
    }


    public enum RegionShape { Rectangular, Ellipsoid }

    public class Region : EntityModelBase
    {
        private string _region_name = "";
        public string region_name
        {
            get { return _region_name; }
            set
            {
                if (_region_name == value)
                    return;
                else
                {
                    _region_name = value;
                    OnPropertyChanged("region_name");
                }
            }
        }
        private RegionShape _region_type = RegionShape.Ellipsoid;
        public RegionShape region_type
        {
            get { return _region_type; }
            set
            {
                if (_region_type == value)
                    return;
                else
                {
                    _region_type = value;
                    OnPropertyChanged("region_type");
                }
            }
        }
        public string region_box_spec_guid_ref { get; set; }
        private bool _region_visibility = true;
        public bool region_visibility
        {
            get { return _region_visibility; }
            set
            {
                if (_region_visibility == value)
                    return;
                else
                {
                    _region_visibility = value;
                    OnPropertyChanged("region_visibility");
                }
            }
        }
        private System.Windows.Media.Color _region_color;
        public System.Windows.Media.Color region_color
        {
            get { return _region_color; }
            set
            {
                if (_region_color == value)
                    return;
                else
                {
                    _region_color = value;
                    OnPropertyChanged("region_color");
                }
            }
        }

        public Region()
        {
            region_name = "Default Region";
            region_box_spec_guid_ref = "";
            region_visibility = true;
            region_color = new System.Windows.Media.Color();
            region_color = System.Windows.Media.Color.FromRgb(255, 255, 255);
        }

        public Region(string name, RegionShape type)
        {
            region_name = name;
            region_type = type;
            region_box_spec_guid_ref = "";
            region_visibility = true;
            region_color = new System.Windows.Media.Color();
            region_color = System.Windows.Media.Color.FromRgb(255, 255, 255);
        }
    }

    public enum ColorList { Red, Orange, Yellow, Green, Blue, Indigo, Violet, Custom, ColorBrewer }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(ColorList), typeof(int))]
    public class ColorListToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return 1;

            try
            {
                int index = (int)value;
                return index;
            }
            catch
            {
                return 1;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return ColorList.Orange;

            int idx = (int)value;
            return (ColorList)Enum.ToObject(typeof(ColorList), (int)idx);
        }
    }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(ColorList), typeof(string))]
    public class ColorListToStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the MoleculeLocation enum...
        private List<string> _color_strings = new List<string>()
                                {
                                    "Red",
                                    "Orange",
                                    "Yellow",
                                    "Green",
                                    "Blue",
                                    "Indigo",
                                    "Violet",
                                    "Custom",
                                    "ColorBrewer"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _color_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return ColorList.Orange;

            int idx = (int)value;
            return (ColorList)Enum.ToObject(typeof(ColorList), (int)idx);
        }
    }


    /// <summary>
    /// get the index of a given color to the index of the predifined colors
    /// </summary>
    public class ColorToListIndexConv : IValueConverter
    {

        private static Color[] pdcolors = new Color[] { Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green, Colors.Blue, Colors.Indigo, Colors.Violet };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var color = (Color)value;
            int index = Array.IndexOf(pdcolors, color);
            return index == -1 ? 7 : index;
        }


        /// <summary>
        /// given index, return color
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return Colors.White;
            int index = (int)value;
            if (index >= 0 && index < 7)
            {
                return pdcolors[index];
            }

            return Colors.White;
        }
    }

    /// <summary>
    /// get the index of a given color to the index of the predifined colors
    /// </summary>
    public class RenderColorToListIndexConv : IMultiValueConverter
    {

        private static Color[] pdcolors = new Color[] { Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green, Colors.Blue, Colors.Indigo, Colors.Violet };

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values[0] == DependencyProperty.UnsetValue)
            {
                return values[0];
            }
            object value = values[0];
            var color = (Color)value;
            int index = Array.IndexOf(pdcolors, color);
            if (index != -1) return index;

            if (values.Length > 1 && values[1] != null)
            {
                var colors = values[1] as ObservableCollection<RenderColor>;
                if (colors != null && ColorHelper.IsColorBrewerColors(colors))
                {
                    return 8;
                }
            }
            else if (values[2] != null)
            {

                var choice = (ColorList)values[2];
                if (choice == ColorList.ColorBrewer)
                {
                    return 8;
                }
            }

            return 7;
        }


        /// <summary>
        /// given index, return color
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return new Object[1] { Colors.White };
            }
            int index = (int)value;
            if (index >= 0 && index < 7)
            {
                return new Object[1] { pdcolors[index] };
            }
            return new Object[1] { Colors.White };
        }
    }




    /// <summary>
    /// Convert color enum to type Color
    /// </summary>
    [ValueConversion(typeof(ColorList), typeof(Color))]
    public class ColorListToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Color col = Color.FromRgb(255, 0, 0);

            if (value == null)
                return col;

            try
            {
                int index = (int)value;
                ColorList colEnum = (ColorList)Enum.ToObject(typeof(ColorList), (int)index);
                //ColorList colEnum = (ColorList)value;

                switch (colEnum)
                {
                    case ColorList.Red:
                        col = Color.FromRgb(255, 0, 0);
                        break;
                    case ColorList.Orange:
                        col = Colors.Orange;
                        break;
                    case ColorList.Yellow:
                        col = Color.FromRgb(255, 255, 0);
                        break;
                    case ColorList.Green:
                        col = Color.FromRgb(0, 255, 0);
                        break;
                    case ColorList.Blue:
                        col = Color.FromRgb(0, 0, 255);
                        break;
                    case ColorList.Indigo:
                        col = Color.FromRgb(64, 0, 192);
                        break;
                    case ColorList.Violet:
                        col = Color.FromRgb(192, 0, 255);
                        break;
                    case ColorList.Custom:
                        col = (Color)parameter;
                        break;
                    default:
                        break;
                }

                return col;
            }
            catch
            {
                return col;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 0;
        }
    }

    [ValueConversion(typeof(Color), typeof(string))]
    public class TextToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string ret = "Red";
            Color col = (Color)value;

            if (col == Colors.Red)
                ret = "Red";
            else if (col == Colors.Orange)
                ret = "Orange";
            else if (col == Colors.Yellow)
                ret = "Yellow";
            else if (col == Colors.Green)
                ret = "Green";
            else if (col == Colors.Blue)
                ret = "Blue";
            else if (col == Colors.Indigo)
                ret = "Indigo";
            else if (col == Colors.Violet)
                ret = "Violet";
            else
                ret = "Custom";

            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public enum MoleculeLocation { Bulk = 0, Boundary }


    [ValueConversion(typeof(object), typeof(bool))]
    public class ObjectToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter == null)
            {
                return value != null ? Visibility.Visible : Visibility.Hidden;
            }
            var option = parameter as string;
            if (option == "Reverse")
            {
                return value == null ? Visibility.Visible : Visibility.Hidden;
            }
            else if (option == "Collapsed")
            {
                return value != null ? Visibility.Visible : Visibility.Collapsed;
            }

            return value != null ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }

    public class DiffSchemeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bResult = true;
            ConfigTransitionScheme ds = value as ConfigTransitionScheme;

            if (ds == null)
            {
                bResult = false;
            }

            return bResult;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConfigTransitionScheme ds = null;

            return ds;
        }
    }

    public class DivDeathDriverToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bResult = true;
            ConfigTransitionDriver dr = value as ConfigTransitionDriver;

            if (dr == null)
            {
                bResult = false;
            }

            return bResult;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConfigTransitionDriver dr = null;

            return dr;
        }
    }

    public class DiffSchemeToDiffNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string name = "";
            ConfigTransitionScheme scheme = value as ConfigTransitionScheme;

            if (scheme != null)
            {
                name = scheme.Name;
            }

            return name;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConfigTransitionScheme scheme = null;

            return scheme;
        }
    }

    /// <summary>
    /// Converter to go between enum values and boolean values for GUI checkbox
    /// </summary>
    [ValueConversion(typeof(MoleculeLocation), typeof(bool))]
    public class MoleculeLocationToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            int n = (int)value;
            if (n == 1)
                return true;
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bVal = (bool)value;
            int idx = 0;
            if (bVal == true)
                idx = 1;

            return (MoleculeLocation)Enum.ToObject(typeof(MoleculeLocation), (int)idx);
        }
    }

    [ValueConversion(typeof(MoleculeLocation), typeof(bool))]
    public class EnumBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            return Enum.Parse(targetType, parameterString);
        }
        #endregion
    }


    public enum BoundaryType { Zero_Flux = 0, Toroidal }

    [ValueConversion(typeof(bool), typeof(int))]
    public class BoolToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return 0;

            return ((bool)value == true) ? 1 : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((int)value == 1) ? true : false;
        }
    }




    public enum BoundaryFace { None = 0, X, Y, Z }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(BoundaryFace), typeof(string))]
    public class BoundaryFaceToShortStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the BoundaryFace enum...
        private List<string> _boundary_face_strings = new List<string>()
                                {
                                    "None",
                                    "X",
                                    "Y",
                                    "Z"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _boundary_face_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _boundary_face_strings.FindIndex(item => item == str);
            return (BoundaryFace)Enum.ToObject(typeof(BoundaryFace), (int)idx);
        }
    }

    /// <summary>
    /// Converter to go between enum values and boolean values for GUI checkbox
    /// </summary>
    [ValueConversion(typeof(BoundaryFace), typeof(string))]
    public class BoundaryFaceToVisStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return "Hidden";

            int n = (int)value;
            if (n != (int)BoundaryFace.None)
                return "Visible";
            else
                return "Hidden";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bVal = (bool)value;
            int idx = 0;
            if (bVal == true)
                idx = 1;

            return (BoundaryFace)Enum.ToObject(typeof(BoundaryFace), (int)idx);
        }
    }

    /// <summary>
    /// base class for applicable config entities
    /// </summary>
    public abstract class ConfigEntity : EntityModelBase
    {
        public ConfigEntity()
        {
            Guid id = Guid.NewGuid();

            entity_guid = id.ToString();
            // initialize time_stamp
        }

        public abstract string GenerateNewName(Level level, string ending);

        public string entity_guid { get; set; }
    }

    /// <summary>
    /// config molecule
    /// </summary>
    public class ConfigMolecule : ConfigEntity, IEquatable<ConfigMolecule>
    {
        public string renderLabel { get; set; }        //label to color scheme

        private string mol_name;
        public string Name
        {
            get
            {
                return mol_name;
            }
            set
            {
                bool bFound = false;  // FindMolecule(value);
                if (bFound == true)
                {
                }
                else
                    mol_name = value;
            }
        }

        private double molWeight;
        public double MolecularWeight
        {
            get
            {
                return molWeight;
            }
            set
            {
                if (molWeight != value)
                {
                    molWeight = value;
                    OnPropertyChanged("MolecularWeight");
                }
            }
        }
        private double effRadius;
        public double EffectiveRadius
        {
            get
            {
                return effRadius;
            }
            set
            {
                if (effRadius != value)
                {
                    effRadius = value;
                    OnPropertyChanged("EffectiveRadius");
                }
            }
        }
        private double diffCoeff;
        public double DiffusionCoefficient
        {
            get
            {
                return diffCoeff;
            }
            set
            {
                if (diffCoeff != value)
                {
                    diffCoeff = value;
                    OnPropertyChanged("DiffusionCoefficient");
                }
            }
        }

        public MoleculeLocation molecule_location { get; set; }

        public ConfigMolecule(string thisName, double thisMW, double thisEffRad, double thisDiffCoeff)
            : base()
        {
            Name = thisName;
            MolecularWeight = thisMW;
            EffectiveRadius = thisEffRad;
            DiffusionCoefficient = thisDiffCoeff;
            molecule_location = MoleculeLocation.Bulk;
            renderLabel = this.entity_guid;
        }

        public ConfigMolecule()
            : base()
        {
            Name = "Molecule_New001"; // +"_" + DateTime.Now.ToString("hhmmssffff");
            MolecularWeight = 1.0;
            EffectiveRadius = 5.0;
            DiffusionCoefficient = 2;
            molecule_location = MoleculeLocation.Bulk;
            renderLabel = this.entity_guid;
        }

        public override string GenerateNewName(Level level, string ending)
        {
            string OriginalName = Name;

            if (OriginalName.Contains(ending))
            {
                int index = OriginalName.IndexOf(ending);
                OriginalName = OriginalName.Substring(0, index);
            }

            int nSuffix = 1;
            string suffix = ending + string.Format("{0:000}", nSuffix);
            string TempMolName = OriginalName + suffix;
            while (FindMoleculeByName(level.entity_repository, TempMolName) == true)
            {
                nSuffix++;
                suffix = ending + string.Format("{0:000}", nSuffix);
                TempMolName = OriginalName + suffix;
            }

            return TempMolName;
        }

        /// <summary>
        /// Need to be able to clone for any Level, not just Protocol
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public ConfigMolecule Clone(Level level)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            ConfigMolecule newmol = JsonConvert.DeserializeObject<ConfigMolecule>(jsonSpec, Settings);

            if (level != null)
            {
                Guid id = Guid.NewGuid();

                newmol.entity_guid = id.ToString();
                newmol.Name = newmol.GenerateNewName(level, "_Copy");
                newmol.renderLabel = newmol.entity_guid;
            }

            return newmol;
        }

        public bool Equals(ConfigMolecule mol)
        {
            if (this.entity_guid != mol.entity_guid)
                return false;
            if (this.diffCoeff != mol.diffCoeff)
                return false;
            if (this.effRadius != mol.effRadius)
                return false;
            if (this.molecule_location != mol.molecule_location)
                return false;
            if (this.molWeight != mol.molWeight)
                return false;
            if (this.Name != mol.Name)
                return false;

            return true;
        }

        public static bool FindMoleculeByName(Protocol protocol, string tempMolName)
        {
            bool ret = false;
            foreach (ConfigMolecule mol in protocol.entity_repository.molecules)
            {
                if (mol.Name.ToLower() == tempMolName.ToLower())
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public static bool FindMoleculeByName(EntityRepository er, string tempMolName)
        {
            bool ret = false;
            foreach (ConfigMolecule mol in er.molecules)
            {
                if (mol.Name.ToLower() == tempMolName.ToLower())
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public void ValidateName(Protocol protocol)
        {
            bool found = false;
            string tempMolName = Name;
            foreach (ConfigMolecule mol in protocol.entity_repository.molecules)
            {
                if (mol.Name == tempMolName && mol.entity_guid != entity_guid)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                Name = GenerateNewName(protocol, "_Copy");
            }
        }

    }

    //public GetMolsInAllRCs

    //  -----------------------------------------------------------------------
    //  Differentiation Schemes
    //

    /// <summary>
    /// Any molecule can be a gene
    /// </summary>
    public class ConfigGene : ConfigEntity, IEquatable<ConfigGene>
    {
        public string Name { get; set; }

        private int copyNumber;
        public int CopyNumber
        {
            get
            {
                return copyNumber;
            }
            set
            {
                if (copyNumber != value)
                {
                    copyNumber = value;
                    OnPropertyChanged("CopyNumber");
                }
            }
        }

        private double activationLevel;
        public double ActivationLevel
        {
            get
            {
                return activationLevel;
            }
            set
            {
                if (activationLevel != value)
                {
                    activationLevel = value;
                    OnPropertyChanged("ActivationLevel");
                }
            }
        }

        public ConfigGene(string name, int copynum, double actlevel)
            : base()
        {
            Name = name;
            CopyNumber = copynum;
            ActivationLevel = actlevel;
        }

        public ConfigGene Clone(Level level)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            ConfigGene newgene = JsonConvert.DeserializeObject<ConfigGene>(jsonSpec, Settings);

            if (level != null)
            {
                Guid id = Guid.NewGuid();

                newgene.entity_guid = id.ToString();
                newgene.Name = newgene.GenerateNewName(level, "_Copy");
            }

            return newgene;
        }

        public override string GenerateNewName(Level level, string ending)
        {
            string OriginalName = Name;

            if (OriginalName.Contains(ending))
            {
                int index = OriginalName.IndexOf(ending);
                OriginalName = OriginalName.Substring(0, index);
            }

            int nSuffix = 1;
            string suffix = ending + string.Format("{0:000}", nSuffix);
            string TempMolName = OriginalName + suffix;
            while (FindGeneByName(level.entity_repository, TempMolName) == true)
            {
                nSuffix++;
                suffix = ending + string.Format("{0:000}", nSuffix);
                TempMolName = OriginalName + suffix;
            }

            return TempMolName;
        }

        public bool Equals(ConfigGene ent)
        {
            if (this.entity_guid != ent.entity_guid)
                return false;
            if (this.activationLevel != ent.activationLevel)
                return false;
            if (this.copyNumber != ent.copyNumber)
                return false;
            if (this.Name != ent.Name)
                return false;

            return true;
        }

        public static bool FindGeneByName(Protocol protocol, string geneName)
        {
            bool ret = false;
            foreach (ConfigGene gene in protocol.entity_repository.genes)
            {
                if (gene.Name == geneName)
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }
        public static bool FindGeneByName(EntityRepository er, string geneName)
        {
            bool ret = false;
            foreach (ConfigGene gene in er.genes)
            {
                if (gene.Name == geneName)
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public void ValidateName(Protocol protocol)
        {
            bool found = false;
            string tempGeneName = Name;
            foreach (ConfigGene gene in protocol.entity_repository.genes)
            {
                if (gene.Name == tempGeneName && gene.entity_guid != entity_guid)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                Name = GenerateNewName(protocol, "_Copy");
            }
        }

    }

    public class ConfigTransitionDriverElement
    {
        //public string driver_element_guid { get; set; }
        public double Alpha { get; set; }
        public double Beta { get; set; }
        public string driver_mol_guid_ref { get; set; }

        public int CurrentState { get; set; }
        public string CurrentStateName { get; set; }
        public int DestState { get; set; }
        public string DestStateName { get; set; }

        public ConfigTransitionDriverElement()
        {
            //Guid id = Guid.NewGuid();
            //driver_element_guid = id.ToString();
            driver_mol_guid_ref = "";
        }
    }

    public class ConfigTransitionDriverRow
    {
        public ObservableCollection<ConfigTransitionDriverElement> elements { get; set; }

        public ConfigTransitionDriverRow()
        {
            elements = new ObservableCollection<ConfigTransitionDriverElement>();
        }
    }

    public class ConfigTransitionDriver : ConfigEntity, IEquatable<ConfigTransitionDriver>
    {
        public string Name { get; set; }
        public DistributedParameter CurrentState { get; set; }
        public string StateName { get; set; }

        public ObservableCollection<ConfigTransitionDriverRow> DriverElements { get; set; }
        public ObservableCollection<string> states { get; set; }

        public ConfigTransitionDriver()
            : base()
        {
            DriverElements = new ObservableCollection<ConfigTransitionDriverRow>();
            states = new ObservableCollection<string>();
            CurrentState = new DistributedParameter(0);
        }

        public ConfigTransitionDriver Clone(bool identical)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);

            ConfigTransitionDriver new_ctd = JsonConvert.DeserializeObject<ConfigTransitionDriver>(jsonSpec, Settings);

            if (identical == false)
            {
                Guid id = Guid.NewGuid();

                new_ctd.entity_guid = id.ToString();
            }

            return new_ctd;
        }

        public override string GenerateNewName(Level level, string ending)
        {
            throw new NotImplementedException();
        }

        public bool Equals(ConfigTransitionDriver ent)
        {
            if (this.entity_guid != ent.entity_guid)
                return false;
            if (this.CurrentState != ent.CurrentState)
                return false;
            if (this.StateName != ent.StateName)
                return false;
            if (this.Name != ent.Name)
                return false;

            return true;
        }
    }

    //A Differentiation Scheme has a name and one list of states, each state with its genes and their boolean values
    //For example, one differentiation scheme's epigenetic map could look like this:
    //
    //    State/Gene     gCXCR5   gsDiv   gsDif1   gsDif2   gIg
    //    ------------------------------------------------------  
    //    Centroblast      0        1        1        0      0
    //    Centrocyte       1        0        0        1      0
    //    Plasmacyte       1        0        0        0      1
    //
    //Its regulators could look like this:
    //
    //    State/State     Centroblast   Centrocyte   Plasmacyte
    //    ------------------------------------------------------  
    //    Centroblast        none         gCXCR5       gIg       
    //    Centrocyte        gsDiv          none       gsDif2        
    //    Plasmacyte        gsDif1        gsDif2       none   

    public class ConfigTransitionScheme : ConfigEntity, IEquatable<ConfigTransitionScheme>
    {
        public string Name { get; set; }

        //For regulators
        public ConfigTransitionDriver Driver { get; set; }

        //Epigenetic map information
        //  Genes (guids) affected by differentiation states
        private ObservableCollection<string> _genes;
        public ObservableCollection<string> genes
        {
            get
            {
                return _genes;
            }
            set
            {
                _genes = value;
                OnPropertyChanged("genes");
            }
        }

        //  Gene activations for each state
        //  The order of states (rows) should match the order in Drive.states
        public ObservableCollection<ConfigActivationRow> activationRows { get; set; }

        public ConfigTransitionScheme()
            : base()
        {
            genes = new ObservableCollection<string>();
            Name = "New diff scheme";
            Driver = new ConfigTransitionDriver();
            activationRows = new ObservableCollection<ConfigActivationRow>();
        }

        private void genes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("genes");
        }

        public override string GenerateNewName(Level level, string ending)
        {
            throw new NotImplementedException();
        }

        public void RemoveActivationRow(ConfigActivationRow row)
        {
            int index = activationRows.IndexOf(row);
            if (index > -1 && index < activationRows.Count)
            {
                activationRows.Remove(row);
                Driver.states.RemoveAt(index);
                Driver.DriverElements.RemoveAt(index);
            }
        }

        public void AddState(string sname)
        {
            //Add a row in Epigenetic Table
            ConfigActivationRow row = new ConfigActivationRow();
            for (int i = 0; i < genes.Count; i++)
            {
                row.activations.Add(1);
            }

            Driver.states.Add(sname);
            activationRows.Add(row);

            OnPropertyChanged("activationRows");

            //Add a row AND a column in Differentiation Table
            ConfigTransitionDriverRow trow;

            //Add a column to existing rows
            for (int k = 0; k < Driver.states.Count - 1; k++)
            {
                trow = Driver.DriverElements[k];
                ConfigTransitionDriverElement e = new ConfigTransitionDriverElement();
                e.Alpha = 0;
                e.Beta = 0;
                e.driver_mol_guid_ref = "";
                e.CurrentStateName = Driver.states[k];
                e.CurrentState = k;
                e.DestState = Driver.states.Count - 1;
                e.DestStateName = Driver.states[Driver.states.Count - 1];
                trow.elements.Add(e);
            }

            //Add a row
            trow = new ConfigTransitionDriverRow();
            for (int j = 0; j < Driver.states.Count; j++)
            {
                ConfigTransitionDriverElement e = new ConfigTransitionDriverElement();
                e.Alpha = 0;
                e.Beta = 0;
                e.driver_mol_guid_ref = "";
                e.CurrentStateName = sname;
                e.CurrentState = Driver.states.Count - 1;
                e.DestState = j;
                e.DestStateName = Driver.states[j];
                trow.elements.Add(e);
            }

            Driver.DriverElements.Add(trow);

            OnPropertyChanged("Driver");
        }

        public ConfigTransitionScheme Clone(bool identical)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);

            ConfigTransitionScheme new_cds = JsonConvert.DeserializeObject<ConfigTransitionScheme>(jsonSpec, Settings);

            if (identical == false)
            {
                Guid id = Guid.NewGuid();

                new_cds.entity_guid = id.ToString();
            }

            return new_cds;
        }

        public bool Equals(ConfigTransitionScheme cts)
        {
            //name
            if (this.Name != cts.Name)
                return false;

            //guid
            if (this.entity_guid != cts.entity_guid)
                return false;

            //driver
            if (this.Driver == null && cts.Driver != null)
                return false;
            else if (this.Driver != null && cts.Driver == null)
                return false;
            else if (this.Driver == null && cts.Driver == null)
            {
            }
            else if (this.Driver.Equals(cts.Driver) == false)
            {
                return false;
            }

            //genes
            if (cts.genes.Count != this.genes.Count)
                return false;

            //Note that here we are depending on the order of genes.
            //If the genes lists have the same genes but in different order, the list is considered NOT equal.
            for (int i = 0; i < this.genes.Count; i++)
            {
                if (this.genes[i] != cts.genes[i])
                {
                    return false;
                }
            }

            //activation rows
            if (this.activationRows.Count != cts.activationRows.Count)
                return false;

            for (int i = 0; i < this.activationRows.Count; i++)
            {
                if (this.activationRows[i].Equals(cts.activationRows[i]) == false)
                    return false;
            }

            return true;

        }
    }

    public class ConfigActivationRow : EntityModelBase, IEquatable<ConfigActivationRow>
    {
        private ObservableCollection<double> _activations;
        public ObservableCollection<double> activations
        {
            get
            {
                return _activations;
            }
            set
            {
                _activations = value;
                OnPropertyChanged("activations");
            }
        }

        public ConfigActivationRow()
        {
            activations = new ObservableCollection<double>();
            activations.CollectionChanged += new NotifyCollectionChangedEventHandler(activations_CollectionChanged);
        }

        private void activations_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("activations");
        }

        public bool Equals(ConfigActivationRow car)
        {
            if (this.activations.Count != car.activations.Count)
                return false;

            //Note that each double value here applies to a gene. 
            //We are expecting them to be in the right order.
            for (int i = 0; i < activations.Count; i++)
            {
                if (activations[i] != car.activations[i])
                    return false;
            }

            return true;
        }
    }

    public enum ExtendedReport { NONE, LEAN, COMPLETE };

    public class ReportMP
    {
        public ExtendedReport mp_extended { get; set; }
        public string molpop_guid_ref { get; set; }

        public ReportMP()
        {
            mp_extended = ExtendedReport.NONE;
        }
    }

    public class ReportECM : ReportMP
    {
        public bool mean { get; set; }

        public ReportECM()
            : base()
        {
            mean = false;
        }
    }

    public enum ReportType { CELL_MP, ECM_MP, VAT_MP };

    // Note: Neumann option may be added later.
    public enum MolBoundaryType { None = 0, Dirichlet, Neumann }
    public enum Boundary { None = 0, left = 1, right, bottom, top, back, front };
    public class BoundaryCondition
    {
        public MolBoundaryType boundaryType { get; set; }
        public Boundary boundary { get; set; }
        public double concVal { get; set; }

        public BoundaryCondition()
        {
        }

        public BoundaryCondition(MolBoundaryType _boundaryType, Boundary _boundary)
        {
            boundaryType = _boundaryType;
            boundary = _boundary;
        }

        public BoundaryCondition(MolBoundaryType _boundaryType, Boundary _boundary, double val)
        {
            boundaryType = _boundaryType;
            boundary = _boundary;
            concVal = val;
        }
    }

    public class ConfigMolecularPopulation : EntityModelBase, IEquatable<ConfigMolecularPopulation>
    {
        public string molpop_guid { get; set; }

        private ConfigMolecule _molecule;
        public ConfigMolecule molecule
        {
            get { return _molecule; }
            set
            {
                _molecule = value;
                if (_molecule != null)
                {
                    renderLabel = _molecule.renderLabel ?? _molecule.entity_guid;
                }
                OnPropertyChanged("molecule");
            }
        }

        private string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                OnPropertyChanged("Name");
            }
        }

        private ReportMP reportMP;
        public ReportMP report_mp
        {
            get { return reportMP; }
            set { reportMP = value; }
        }

        private MolPopDistribution _mp_distribution;
        public MolPopDistribution mp_distribution
        {
            get { return _mp_distribution; }
            set
            {
                if (_mp_distribution == value)
                    return;
                else
                {
                    _mp_distribution = value;
                    OnPropertyChanged("mp_distribution");
                }
            }
        }

        public string renderLabel { get; set; }        //label to color scheme


        public ConfigMolecularPopulation()
        {
        }

        public ConfigMolecularPopulation(ReportType rt)
        {
            Guid id = Guid.NewGuid();
            molpop_guid = id.ToString();

            if (rt == ReportType.CELL_MP || rt == ReportType.VAT_MP)
            {
                reportMP = new ReportMP();
            }
            else if (rt == ReportType.ECM_MP)
            {
                reportMP = new ReportECM();
            }
            else
            {
                throw new Exception("Undefined report type in ConfigMolecularPopulation.");
            }
            reportMP.molpop_guid_ref = molpop_guid;

            mp_distribution = new MolPopHomogeneousLevel();
        }

        /// <summary>
        /// push a molecule into this molpop
        /// </summary>
        /// <param name="m">the molecule</param>
        public void pushMolecule(ConfigMolecule m)
        {
            if (molecule.entity_guid == m.entity_guid)
            {
                molecule = m;
            }
        }

        public bool Equals(ConfigMolecularPopulation molpop)
        {
            if (this.molpop_guid != molpop.molpop_guid)
                return false;

            if (this.Name != molpop.Name)
                return false;

            if (this.renderLabel != molpop.renderLabel)
                return false;

            if (this.molecule.entity_guid != molpop.molecule.entity_guid)
                return false;

            if (this.mp_distribution.mp_distribution_type != molpop.mp_distribution.mp_distribution_type)
                return false;

            if (this.mp_distribution.Equals(molpop.mp_distribution) == false)
                return false;

            return true;
        }
    }

    public class ConfigCompartment : EntityModelBase
    {
        // private to Protocol; see comment in EntityRepository
        public ObservableCollection<ConfigMolecularPopulation> molpops { get; set; }
        private ObservableCollection<ConfigReaction> _reactions;
        public ObservableCollection<ConfigReaction> Reactions
        {
            get { return _reactions; }
            set
            {
                if (_reactions == value)
                    return;
                else
                {
                    _reactions = value;
                    OnPropertyChanged("Reactions");
                }
            }
        }
        public ObservableCollection<ConfigReactionComplex> reaction_complexes { get; set; }

        [JsonIgnore]
        public Dictionary<string, ConfigMolecularPopulation> molpops_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigMolecule> molecules_dict;  //key=molecule_guid(string), value=ConfigMolecule
        [JsonIgnore]
        public Dictionary<string, ConfigReaction> reactions_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigReactionComplex> reaction_complexes_dict;

        private int gaussRetrieve;

        public ConfigCompartment()
        {
            molpops = new ObservableCollection<ConfigMolecularPopulation>();
            _reactions = new ObservableCollection<ConfigReaction>();
            reaction_complexes = new ObservableCollection<ConfigReactionComplex>();
            molpops_dict = new Dictionary<string, ConfigMolecularPopulation>();
            molecules_dict = new Dictionary<string, ConfigMolecule>();
            reactions_dict = new Dictionary<string, ConfigReaction>();
            reaction_complexes_dict = new Dictionary<string, ConfigReactionComplex>();

            molpops.CollectionChanged += new NotifyCollectionChangedEventHandler(molpops_CollectionChanged);
            _reactions.CollectionChanged += new NotifyCollectionChangedEventHandler(reactions_CollectionChanged);
            reaction_complexes.CollectionChanged += new NotifyCollectionChangedEventHandler(reaction_complexes_CollectionChanged);
        }

        /// <summary>
        /// reset the counter
        /// </summary>
        public void resetGaussRetrieve()
        {
            gaussRetrieve = 0;
        }

        /// <summary>
        /// grab the next Gaussian spec
        /// </summary>
        /// <returns>the spec or null when done</returns>
        public GaussianSpecification nextGaussSpec()
        {
            while (gaussRetrieve < molpops.Count && molpops[gaussRetrieve].mp_distribution is MolPopGaussian == false)
            {
                gaussRetrieve++;
            }
            // it's a Gaussian
            if (gaussRetrieve < molpops.Count)
            {
                GaussianSpecification next = ((MolPopGaussian)molpops[gaussRetrieve].mp_distribution).gauss_spec;

                gaussRetrieve++;
                return next;
            }
            return null;
        }

        /// <summary>
        /// Add a molecular population to a compartment, given a molecule.
        /// Meant to be used for a new or cloned ConfigMolecule.
        /// </summary>
        /// <param name="mol"></param>
        /// <param name="comp"></param>
        /// <param name="isCell"></param>
        public void AddMolPop(ConfigMolecule mol, Boolean isCell)
        {
            if (molecules_dict.ContainsKey(mol.entity_guid) == true)
                return;

            ConfigMolecularPopulation cmp;

            if (isCell == true)
            {
                cmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
            }
            else
            {
                cmp = new ConfigMolecularPopulation(ReportType.ECM_MP);
            }
            cmp.molecule = mol.Clone(null);
            cmp.Name = mol.Name;
            molpops.Add(cmp);
        }

        /// <summary>
        /// push a molecule into this compartment
        /// </summary>
        /// <param name="m">the molecule</param>
        public void pushMolecule(ConfigMolecule m)
        {
            if (molecules_dict.ContainsKey(m.entity_guid) == true)
            {
                molecules_dict[m.entity_guid] = m;
            }
            foreach (ConfigMolecularPopulation mp in molpops)
            {
                mp.pushMolecule(m);
                // should always be in the dictionary also, but check for safety
                if (molpops_dict.ContainsKey(mp.molpop_guid) == true)
                {
                    molpops_dict[mp.molpop_guid].pushMolecule(m);
                }
            }
        }

        /// <summary>
        /// push a reaction into this compartment
        /// </summary>
        /// <param name="r">the reaction</param>
        public void pushReaction(ConfigReaction r)
        {
            for (int i = 0; i < Reactions.Count; i++)
            {
                if (Reactions[i].entity_guid == r.entity_guid)
                {
                    Reactions[i] = r;
                    // should always be in the dictionary also, but check for safety
                    if (reactions_dict.ContainsKey(r.entity_guid) == true)
                    {
                        reactions_dict[r.entity_guid] = r;
                    }
                }
            }
        }

        /// <summary>
        /// push a reaction complex into this compartment
        /// </summary>
        /// <param name="rc">the reaction complex</param>
        public void pushReactionComplex(ConfigReactionComplex rc)
        {
            for (int i = 0; i < reaction_complexes.Count; i++)
            {
                if (reaction_complexes[i].entity_guid == rc.entity_guid)
                {
                    reaction_complexes[i] = rc;
                    // should always be in the dictionary also, but check for safety
                    if (reaction_complexes_dict.ContainsKey(rc.entity_guid) == true)
                    {
                        reaction_complexes_dict[rc.entity_guid] = rc;
                    }
                }
            }
        }

        private void reactions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigReaction cr = nn as ConfigReaction;

                    if (reactions_dict.ContainsKey(cr.entity_guid) == false)
                    {
                        reactions_dict.Add(cr.entity_guid, cr);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReaction cr = dd as ConfigReaction;

                    if (reactions_dict.ContainsKey(cr.entity_guid) == true)
                    {
                        reactions_dict.Remove(cr.entity_guid);
                    }
                }
            }
        }

        private void molpops_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigMolecularPopulation mp = nn as ConfigMolecularPopulation;
                    //mp.PropertyChanged += mp_PropertyChanged;

                    // add molpop into molpops_dict
                    if (molpops_dict.ContainsKey(mp.molpop_guid) == false)
                    {
                        molpops_dict.Add(mp.molpop_guid, mp);
                    }
                    if (molecules_dict.ContainsKey(mp.molecule.entity_guid) == false)
                    {
                        molecules_dict.Add(mp.molecule.entity_guid, mp.molecule);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigMolecularPopulation mp = dd as ConfigMolecularPopulation;
                    mp.PropertyChanged -= mp_PropertyChanged;

                    // remove from molpops_dict
                    if (molpops_dict.ContainsKey(mp.molpop_guid) == true)
                    {
                        molpops_dict.Remove(mp.molpop_guid);
                    }
                    if (molecules_dict.ContainsKey(mp.molecule.entity_guid) == true)
                    {
                        molecules_dict.Remove(mp.molecule.entity_guid);
                    }
                }
            }

            OnPropertyChanged("molpops");
        }

        private void reaction_complexes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigReactionComplex rc = nn as ConfigReactionComplex;

                    // add
                    if (reaction_complexes_dict.ContainsKey(rc.entity_guid) == false)
                    {
                        reaction_complexes_dict.Add(rc.entity_guid, rc);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReactionComplex rc = dd as ConfigReactionComplex;

                    // remove
                    if (reaction_complexes_dict.ContainsKey(rc.entity_guid) == true)
                    {
                        reaction_complexes_dict.Remove(rc.entity_guid);
                    }
                }
            }
        }

        /// <summary>
        /// rebuild the molecules_dict that is used to screen reactions available to add
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "molecule")
            {
                return;
            }
            molecules_dict.Clear();
            foreach (ConfigMolecularPopulation mp in molpops)
            {
                if (molecules_dict.ContainsKey(mp.molecule.entity_guid) == false)
                {
                    molecules_dict.Add(mp.molecule.entity_guid, mp.molecule);
                }
            }
        }

        /// <summary>
        /// get a reaction with a specified guid
        /// </summary>
        /// <param name="guid">guid for lookup</param>
        /// <returns>null if unsuccessful, the reaction otherwise</returns>
        public ConfigReaction GetReaction(string guid)
        {
            if (reactions_dict.ContainsKey(guid) == true)
            {
                return reactions_dict[guid];
            }
            return null;
        }

        //Return true if this compartment has a molecular population with given molecule
        public bool HasMolecule(ConfigMolecule mol)
        {
            if (molecules_dict.ContainsKey(mol.entity_guid))
            {
                return true;
            }
            return false;
        }

        //Return true if this compartment has a molecular population with given molecule guid
        public bool HasMolecule(string molguid)
        {
            if (molecules_dict.ContainsKey(molguid))
            {
                return true;
            }
            return false;
        }

        //Return true if this compartment has all the molecules in the given list of molecule guids
        public bool HasMolecules(ObservableCollection<string> mol_guid_refs)
        {
            bool res = true;
            foreach (string molguid in mol_guid_refs)
            {
                if (!HasMolecule(molguid))
                {
                    res = false;
                    break;
                }
            }
            return res;
        }

        //Remove a molecular population given a molecule guid
        public void RemoveMolecularPopulation(string molecule_guid)
        {
            string molpop_guid = "";

            ConfigMolecularPopulation delMolPop = null;
            foreach (ConfigMolecularPopulation cmp in molpops)
            {
                if (molecule_guid == cmp.molecule.entity_guid)
                {
                    molpop_guid = cmp.molpop_guid;
                    delMolPop = cmp;
                    break;
                }
            }

            if (molpop_guid.Length > 0)
            {
                molpops.Remove(delMolPop);
            }
        }
    }

    public enum ReactionType
    {
        Association = 0, Dissociation, Annihilation, Dimerization, DimerDissociation,
        Transformation, AutocatalyticTransformation, CatalyzedAnnihilation,
        CatalyzedAssociation, CatalyzedCreation, CatalyzedDimerization, CatalyzedDimerDissociation,
        CatalyzedTransformation, CatalyzedDissociation, CatalyzedBoundaryActivation, BoundaryAssociation,
        BoundaryDissociation, Generalized, BoundaryTransportFrom, BoundaryTransportTo,
        Transcription
    }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(ReactionType), typeof(string))]
    public class ReactionTypeToShortStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> _reaction_type_strings = new List<string>()
                                {
                                    "Association",
                                    "Dissociation",
                                    "Annihilation",
                                    "Dimerization",
                                    "DimerDissociation",
                                    "Transformation",
                                    "AutocatalyticTransformation",
                                    "CatalyzedAnnihilation",
                                    "CatalyzedAssociation",
                                    "CatalyzedCreation",
                                    "CatalyzedDimerization",
                                    "CatalyzedDimerDissociation",
                                    "CatalyzedTransformation",
                                    "CatalyzedDissociation",
                                    "CatalyzedBoundaryActivation",
                                    "BoundaryAssociation",
                                    "BoundaryDissociation",
                                    "Generalized",
                                    "BoundaryTransportTo",
                                    "BoundaryTransportFrom",
                                    "Transcription"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _reaction_type_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _reaction_type_strings.FindIndex(item => item == str);
            return (ReactionType)Enum.ToObject(typeof(ReactionType), (int)idx);
        }
    }

    public class ConfigReaction : ConfigEntity, IEquatable<ConfigReaction>
    {
        public ConfigReaction()
            : base()
        {
            rate_const = 0;

            reactants_molecule_guid_ref = new ObservableCollection<string>();
            products_molecule_guid_ref = new ObservableCollection<string>();
            modifiers_molecule_guid_ref = new ObservableCollection<string>();
        }

        public ConfigReaction(ConfigReaction reac)
            : base()
        {
            reaction_template_guid_ref = reac.reaction_template_guid_ref;

            rate_const = reac.rate_const;

            reactants_molecule_guid_ref = new ObservableCollection<string>();
            products_molecule_guid_ref = new ObservableCollection<string>();
            modifiers_molecule_guid_ref = new ObservableCollection<string>();

            reactants_molecule_guid_ref = reac.reactants_molecule_guid_ref;
            products_molecule_guid_ref = reac.products_molecule_guid_ref;
            modifiers_molecule_guid_ref = reac.modifiers_molecule_guid_ref;
        }

        /// <summary>
        /// create a clone of a reaction
        /// </summary>
        /// <param name="identical">true to create a literal copy</param>
        /// <returns></returns>
        public ConfigReaction Clone(bool identical)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            ConfigReaction newreaction = JsonConvert.DeserializeObject<ConfigReaction>(jsonSpec, Settings);

            if (identical == false)
            {
                Guid id = Guid.NewGuid();

                newreaction.entity_guid = id.ToString();
            }
            return newreaction;
        }

        public bool Equals(ConfigReaction r)
        {
            if (this.entity_guid != r.entity_guid)
                return false;
            if (this.rate_const != r.rate_const)
                return false;
            if (this.TotalReactionString != r.TotalReactionString)
                return false;

            return true;
        }

        public override string GenerateNewName(Level level, string ending)
        {
            throw new NotImplementedException();
        }

        public void GetTotalReactionString(EntityRepository repos)
        {
            string s = "";
            int i = 0;

            // Reactants
            foreach (string mol_guid_ref in reactants_molecule_guid_ref)
            {
                //stoichiometry
                int n = repos.reaction_templates_dict[reaction_template_guid_ref].reactants_stoichiometric_const[i];
                i++;
                if (n > 1)
                    s += n;
                s += repos.molecules_dict[mol_guid_ref].Name;
                s += " + ";
            }

            i = 0;
            // Modifiers
            foreach (string mol_guid_ref in modifiers_molecule_guid_ref)
            {
                //stoichiometry
                int n = repos.reaction_templates_dict[reaction_template_guid_ref].modifiers_stoichiometric_const[i];
                i++;
                if (n > 1)
                    s += n;

                if (repos.genes_dict.ContainsKey(mol_guid_ref))
                {
                    s += repos.genes_dict[modifiers_molecule_guid_ref[0]].Name;
                }
                else
                {
                    s += repos.molecules_dict[mol_guid_ref].Name;
                }
                s += " + ";
            }

            char[] trimChars = { ' ', '+' };
            s = s.Trim(trimChars);

            s = s + " -> ";

            i = 0;
            // Products
            foreach (string mol_guid_ref in products_molecule_guid_ref)
            {
                //stoichiometry
                int n = repos.reaction_templates_dict[reaction_template_guid_ref].products_stoichiometric_const[i];
                i++;
                if (n > 1)
                    s += n;

                s += repos.molecules_dict[mol_guid_ref].Name;
                s += " + ";
            }
            i = 0;
            // Modifiers
            foreach (string mol_guid_ref in modifiers_molecule_guid_ref)
            {
                //stoichiometry
                int n = repos.reaction_templates_dict[reaction_template_guid_ref].modifiers_stoichiometric_const[i];
                i++;
                if (n > 1)
                    s += n;

                if (repos.genes_dict.ContainsKey(mol_guid_ref))
                {
                    s += repos.genes_dict[modifiers_molecule_guid_ref[0]].Name;
                }
                else
                {
                    s += repos.molecules_dict[mol_guid_ref].Name;
                }
                s += " + ";
            }

            s = s.Trim(trimChars);
            TotalReactionString = s;
        }

        public bool HasMolecule(string molguid)
        {
            if (reactants_molecule_guid_ref.Contains(molguid) || products_molecule_guid_ref.Contains(molguid) || modifiers_molecule_guid_ref.Contains(molguid))
            {
                return true;
            }

            return false;
        }

        public bool HasBoundaryMolecule(EntityRepository repos)
        {
            foreach (string molguid in reactants_molecule_guid_ref)
            {
                if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    return true;
            }
            foreach (string molguid in products_molecule_guid_ref)
            {
                if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    return true;
            }
            foreach (string molguid in modifiers_molecule_guid_ref)
            {
                if (!repos.genes_dict.ContainsKey(molguid))
                {
                    if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                        return true;
                }
            }

            return false;
        }

        public bool HasBulkMolecule(EntityRepository repos)
        {
            foreach (string molguid in reactants_molecule_guid_ref)
            {
                if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Bulk)
                    return true;
            }
            foreach (string molguid in products_molecule_guid_ref)
            {
                if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Bulk)
                    return true;
            }
            foreach (string molguid in modifiers_molecule_guid_ref)
            {
                if (!repos.genes_dict.ContainsKey(molguid))
                {
                    if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Bulk)
                        return true;
                }
            }

            return false;
        }

        public bool HasGene(EntityRepository repos)
        {
            foreach (string molguid in modifiers_molecule_guid_ref)
            {
                if (repos.genes_dict.ContainsKey(molguid))
                {
                    return true;
                }
            }

            return false;
        }    

        public bool IsBoundaryReaction(EntityRepository repos)
        {
            if (HasBoundaryMolecule(repos) == true && HasBulkMolecule(repos) == true)
                return true;

            return false;
        }

        public string reaction_template_guid_ref { get; set; }

        private double _rate_const;
        public double rate_const
        {
            get
            {
                return _rate_const;
            }
            set
            {
                _rate_const = value;
                OnPropertyChanged("rate_const");
            }
        }

        // hold the molecule_guid_refs of the {reactant|product|modifier} molpops
        public ObservableCollection<string> reactants_molecule_guid_ref;
        public ObservableCollection<string> products_molecule_guid_ref;
        public ObservableCollection<string> modifiers_molecule_guid_ref;

        public string TotalReactionString { get; set; }

        public ObservableCollection<string> GetBulkMolecules(EntityRepository repos)
        {
            ObservableCollection<string> bulkMols = new ObservableCollection<string>();

            foreach (string molguid in reactants_molecule_guid_ref)
            {
                if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Bulk)
                    bulkMols.Add(molguid);
            }
            foreach (string molguid in products_molecule_guid_ref)
            {
                if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Bulk)
                    bulkMols.Add(molguid);
            }
            foreach (string molguid in modifiers_molecule_guid_ref)
            {
                if (!repos.genes_dict.ContainsKey(molguid))
                {
                    if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Bulk)
                        bulkMols.Add(molguid);
                }
            }

            return bulkMols;
        }

        public ObservableCollection<string> GetBoundaryMolecules(EntityRepository repos)
        {
            ObservableCollection<string> boundaryMols = new ObservableCollection<string>();

            foreach (string molguid in reactants_molecule_guid_ref)
            {
                if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    boundaryMols.Add(molguid);
            }
            foreach (string molguid in products_molecule_guid_ref)
            {
                if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    boundaryMols.Add(molguid);
            }
            foreach (string molguid in modifiers_molecule_guid_ref)
            {
                if (!repos.genes_dict.ContainsKey(molguid))
                {
                    if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                        boundaryMols.Add(molguid);
                }
            }
            return boundaryMols;
        }


    }

    public class ConfigReactionTemplate : ConfigEntity, IEquatable<ConfigReactionTemplate>
    {
        public string name;
        // stoichiometric constants
        public ObservableCollection<int> reactants_stoichiometric_const;
        public ObservableCollection<int> products_stoichiometric_const;
        public ObservableCollection<int> modifiers_stoichiometric_const;
        //reaction type
        public ReactionType reac_type { get; set; }
        // True if the reaction involves bulk and boundary molecules. Default is false.
        public bool isBoundary;

        public ConfigReactionTemplate()
            : base()
        {
            reactants_stoichiometric_const = new ObservableCollection<int>();
            products_stoichiometric_const = new ObservableCollection<int>();
            modifiers_stoichiometric_const = new ObservableCollection<int>();
            isBoundary = false;
        }

        public override string GenerateNewName(Level level, string ending)
        {
            throw new NotImplementedException();
        }

        public ConfigReactionTemplate Clone(bool identical)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            ConfigReactionTemplate newRT = JsonConvert.DeserializeObject<ConfigReactionTemplate>(jsonSpec, Settings);

            if (identical == false)
            {
                Guid id = Guid.NewGuid();
                newRT.entity_guid = id.ToString();
            }

            return newRT;
        }

        public bool Equals(ConfigReactionTemplate crt)
        {
            if (this.entity_guid != crt.entity_guid)
                return false;

            if (this.reac_type != crt.reac_type)
                return false;

            return true;
        }

    }

    public class ConfigReactionComplex : ConfigEntity, IEquatable<ConfigReactionComplex>
    {
        private string rcName;
        public string Name
        {
            get
            {
                return rcName;
            }

            set
            {
                if (rcName != value)
                {
                    rcName = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        private ObservableCollection<ConfigReaction> _reactions;
        public ObservableCollection<ConfigReaction> reactions
        {
            get
            {
                return _reactions;
            }
            set
            {
                _reactions = value;
                OnPropertyChanged("reactions");
            }
        }

        public ObservableCollection<ConfigMolecularPopulation> molpops { get; set; }

        [JsonIgnore]
        public Dictionary<string, ConfigReaction> reactions_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigMolecule> molecules_dict;

        public ConfigReactionComplex()
            : this("NewRC")
        {
        }

        public ConfigReactionComplex(string name)
            : base()
        {
            Name = name;
            reactions = new ObservableCollection<ConfigReaction>();
            molpops = new ObservableCollection<ConfigMolecularPopulation>();
            reactions_dict = new Dictionary<string, ConfigReaction>();
            reactions.CollectionChanged += new NotifyCollectionChangedEventHandler(reactions_CollectionChanged);
            molecules_dict = new Dictionary<string, ConfigMolecule>();
            molpops.CollectionChanged += new NotifyCollectionChangedEventHandler(molpops_CollectionChanged);
        }

        private void reactions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigReaction cr = nn as ConfigReaction;
                    if (reactions_dict.ContainsKey(cr.entity_guid) == false)
                    {
                        reactions_dict.Add(cr.entity_guid, cr);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReaction cr = dd as ConfigReaction;

                    if (reactions_dict.ContainsKey(cr.entity_guid) == true)
                    {
                        reactions_dict.Remove(cr.entity_guid);
                    }
                }
            }

        }

        private void molpops_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigMolecularPopulation cm = nn as ConfigMolecularPopulation;

                    if (molecules_dict.ContainsKey(cm.molecule.entity_guid) == false)
                    {
                        molecules_dict.Add(cm.molecule.entity_guid, cm.molecule);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigMolecularPopulation cm = dd as ConfigMolecularPopulation;

                    if (molecules_dict.ContainsKey(cm.molecule.entity_guid) == true)
                    {
                        molecules_dict.Remove(cm.molecule.entity_guid);
                    }
                }
            }
        }

        /// <summary>
        /// push a reaction into this reaction complex
        /// </summary>
        /// <param name="r">the reaction</param>
        public void pushReaction(ConfigReaction r)
        {
            for (int i = 0; i < reactions.Count; i++)
            {
                if (reactions[i].entity_guid == r.entity_guid)
                {
                    reactions[i] = r;
                    // should always be in the dictionary also, but check for safety
                    if (reactions_dict.ContainsKey(r.entity_guid) == true)
                    {
                        reactions_dict[r.entity_guid] = r;
                    }
                }
            }
        }

        /// <summary>
        /// push a molecule into this reaction comlex
        /// </summary>
        /// <param name="m">the molecule</param>
        public void pushMolecule(ConfigMolecule m)
        {
            if (molecules_dict.ContainsKey(m.entity_guid) == true)
            {
                molecules_dict[m.entity_guid] = m;
            }
            foreach (ConfigMolecularPopulation mp in molpops)
            {
                mp.pushMolecule(m);
                // if we ever add a dictionary for molpops, enable this
                /*
                // should always be in the dictionary also, but check for safety
                if (molpops_dict.ContainsKey(mp.molpop_guid) == true)
                {
                    molpops_dict[mp.molpop_guid].pushMolecule(m, forced);
                }*/
            }
        }

        public ConfigReactionComplex Clone(bool identical)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);

            ConfigReactionComplex newrc = JsonConvert.DeserializeObject<ConfigReactionComplex>(jsonSpec, Settings);

            if (identical == false)
            {
                Guid id = Guid.NewGuid();
                newrc.entity_guid = id.ToString();
            }
            return newrc;
        }

        public bool Equals(ConfigReactionComplex crc)
        {
            if (this.Name != crc.Name)
                return false;

            if (this.entity_guid != crc.entity_guid)
                return false;

            //Check reactions
            if (reactions.Count != crc.reactions.Count)
                return false;

            foreach (ConfigReaction reac in reactions)
            {
                if (crc.reactions_dict.ContainsKey(reac.entity_guid) == false)
                {
                    return false;
                }
                else
                {
                    ConfigReaction reac2 = crc.reactions_dict[reac.entity_guid];
                    if (reac.Equals(reac2) == false)
                        return false;
                }
            }

            //Check molpops
            if (crc.molpops.Count != this.molpops.Count)
                return false;

            for (int i = 0; i < this.molpops.Count; i++)
            {
                if (this.molpops[i].Equals(crc.molpops[i]) == false)
                    return false;
            }
            
            //Check molecules
            if (crc.molecules_dict.Count != this.molecules_dict.Count)
                return false;

            foreach (KeyValuePair<string, ConfigMolecule> kvp in molecules_dict)
            {
                if (crc.molecules_dict.ContainsKey(kvp.Key) == false)
                    return false;
                else
                {
                    ConfigMolecule mol = crc.molecules_dict[kvp.Key];
                    if (mol.Equals(kvp.Value) == false)
                        return false;
                }
            }

            return true;
        }

        public void ValidateName(Protocol protocol)
        {
            bool found = false;
            string tempRCName = Name;
            foreach (ConfigReactionComplex crc in protocol.entity_repository.reaction_complexes)
            {
                if (crc.Name == tempRCName && crc.entity_guid != entity_guid)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                Name = GenerateNewName(protocol, "_Copy");
            }
        }

        public override string GenerateNewName(Level level, string ending)
        {
            string OriginalName = Name;

            if (OriginalName.Contains(ending))
            {
                int index = OriginalName.IndexOf(ending);
                OriginalName = OriginalName.Substring(0, index);
            }

            int nSuffix = 1;
            string suffix = ending + string.Format("{0:000}", nSuffix);
            string TempRCName = OriginalName + suffix;
            while (FindByName(level, TempRCName) == true)
            {
                nSuffix++;
                suffix = ending + string.Format("{0:000}", nSuffix);
                TempRCName = OriginalName + suffix;
            }

            return TempRCName;
        }

        public static bool FindByName(Level level, string name)
        {
            bool ret = false;
            foreach (ConfigReactionComplex crc in level.entity_repository.reaction_complexes)
            {
                if (crc.Name == name)
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        private bool HasMolecule(string guid)
        {
            return molecules_dict.ContainsKey(guid);
        }

        private void CreateReactionMolpops(ConfigReaction reac, ObservableCollection<string> mols, EntityRepository er)
        {
            foreach (string molguid in mols)
            {
                if (molecules_dict.ContainsKey(molguid) == false)
                {
                    ConfigMolecule configMolecule = er.molecules_dict[molguid];
                    //ConfigMolecule configMolecule = molecules_dict[molguid];                 
                    if (configMolecule != null)
                    {
                        ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);
                        configMolPop.molecule = configMolecule.Clone(null);
                        configMolPop.molecule.entity_guid = configMolecule.entity_guid;
                        configMolPop.Name = configMolecule.Name;

                        MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();

                        hl.concentration = 0;
                        configMolPop.mp_distribution = hl;
                        molpops.Add(configMolPop);
                    }
                }
            }
        }

        public void RemoveReaction(ConfigReaction cr)
        {
            //remove reaction
            reactions.Remove(cr);

            //remove molpops whose molecules are not in the remaining reactions
            ObservableCollection<ConfigMolecularPopulation> newmolpops = new ObservableCollection<ConfigMolecularPopulation>();

            //Create a new molpops collection from the current reactions in the complex (molpops)
            //We cannot lose the attributes of the existing mol pops so we have to do it this way
            foreach (ConfigReaction reac in reactions)
            {
                AddMolPop(newmolpops, reac.reactants_molecule_guid_ref);
                AddMolPop(newmolpops, reac.products_molecule_guid_ref);
                AddMolPop(newmolpops, reac.modifiers_molecule_guid_ref);
            }
            molpops = newmolpops;

            //recreate molecules_dict
            molecules_dict.Clear();
            foreach (ConfigMolecularPopulation molpop in molpops)
            {               
                molecules_dict.Add(molpop.molecule.entity_guid, molpop.molecule);
            }
            molpops = newmolpops;

            //recreate molecules_dict
            molecules_dict.Clear();
            foreach (ConfigMolecularPopulation molpop in molpops)
            {
                molecules_dict.Add(molpop.molecule.entity_guid, molpop.molecule);
            }

        }

        /// <summary>
        /// This copies existing molpops into the new list after user has deleted reactions from reaction complex).
        /// It is only called when reactions are removed.
        /// </summary>
        /// <param name="newmolpops"></param>
        /// <param name="guid_refs"></param>
        private void AddMolPop(ObservableCollection<ConfigMolecularPopulation> newmolpops, ObservableCollection<string> guid_refs)
        {
            foreach (string guid in guid_refs)
            {
                ConfigMolecularPopulation cmp = molpops.Where(m => m.molecule.entity_guid == guid).First();
                if (newmolpops.Contains(cmp) == false)
                    newmolpops.Add(cmp);
            }
        }

        /// <summary>
        /// This creates new mol pops for a reaction complex but it initializes the properties to default values.
        /// This is only called when a reaction is added to a reaction complex.
        /// </summary>
        /// <param name="reac"></param>
        /// <param name="er"></param>
        public void AddReactionMolPops(ConfigReaction reac, EntityRepository er)
        {
            CreateReactionMolpops(reac, reac.reactants_molecule_guid_ref, er);
            CreateReactionMolpops(reac, reac.products_molecule_guid_ref, er);
            CreateReactionMolpops(reac, reac.modifiers_molecule_guid_ref, er);
        }
    }

    public class ConfigCell : ConfigEntity, IEquatable<ConfigCell>
    {
        public string renderLabel { get; set; }        //label to color scheme

        public ConfigCell()
            : base()
        {
            CellName = "Default Cell";
            CellRadius = 5.0;

            TransductionConstant = new DistributedParameter(0.0);
            DragCoefficient = new DistributedParameter(1.0);
            Sigma = new DistributedParameter(0.0);

            membrane = new ConfigCompartment();
            cytosol = new ConfigCompartment();
            locomotor_mol_guid_ref = "";

            // behaviors
            genes = new ObservableCollection<ConfigGene>();

            renderLabel = this.entity_guid;
        }

        public ConfigCell Clone(bool identical)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            ConfigCell newcell = JsonConvert.DeserializeObject<ConfigCell>(jsonSpec, Settings);

            if (identical == false)
            {
                Guid id = Guid.NewGuid();

                newcell.entity_guid = id.ToString();
                newcell.renderLabel = newcell.entity_guid;
            }
            return newcell;
        }

        private string cellName;
        public string CellName
        {
            get
            {
                return cellName;
            }

            set
            {
                cellName = value;
                OnPropertyChanged("CellName");
            }
        }

        private double cellRadius;
        public double CellRadius
        {
            get
            {
                return cellRadius;
            }
            set
            {
                cellRadius = value;
                OnPropertyChanged("CellRadius");
            }
        }

        private string _locomotor_mol_guid_ref;
        public string locomotor_mol_guid_ref
        {
            get
            {
                return _locomotor_mol_guid_ref;
            }
            set
            {
                if (value == null)
                {
                    _locomotor_mol_guid_ref = "";
                }
                else
                {
                    _locomotor_mol_guid_ref = value;
                }
            }
        }

        private DistributedParameter transductionConstant;
        public DistributedParameter TransductionConstant
        {
            get
            {
                return transductionConstant;
            }
            set
            {
                transductionConstant = value;
                OnPropertyChanged("TransductionConstant");
            }
        }

        private DistributedParameter dragCoefficient;
        public DistributedParameter DragCoefficient
        {
            get
            {
                return dragCoefficient;
            }
            set
            {
                dragCoefficient = value;
                OnPropertyChanged("DragCoefficient");
            }
        }

        /// <summary>
        /// Parameter for stochastic force
        /// </summary>
        private DistributedParameter sigma;
        public DistributedParameter Sigma
        {
            get
            {
                return sigma;
            }
            set
            {
                sigma = value;
                OnPropertyChanged("Sigma");
            }
        }

        public ConfigCompartment membrane { get; set; }
        public ConfigCompartment cytosol { get; set; }

        public ObservableCollection<ConfigGene> genes { get; set; }

        private ConfigTransitionScheme _diff_scheme;
        public ConfigTransitionScheme diff_scheme
        {
            get
            {
                return _diff_scheme;
            }

            set
            {
                _diff_scheme = value;
                OnPropertyChanged("diff_scheme");
            }
        }

        // ConfigTransitionDriver contains ConfigTransitionDriverElement
        // ConfigTransitionDriverElement contains information about 
        //      signaling molecule that drives cell death and alphas and betas
        private ConfigTransitionDriver _death_driver;
        public ConfigTransitionDriver death_driver
        {
            get
            {
                return _death_driver;
            }

            set
            {
                _death_driver = value;
                OnPropertyChanged("death_driver");
            }
        }

        private ConfigTransitionScheme _div_scheme;
        public ConfigTransitionScheme div_scheme
        {
            get
            {
                return _div_scheme;
            }

            set
            {
                _div_scheme = value;
                OnPropertyChanged("div_scheme");
            }
        }

        public int CurrentDeathState;
        public int CurrentDivState;

        //Return true if this compartment has a molecular population with given molecule
        public bool HasGene(string gene_guid)
        {
            foreach (ConfigGene gene in genes)
            {
                if (gene.entity_guid == gene_guid)
                {
                    return true;
                }
            }

            return false;
        }

        //Return true if this cell has all the genes in the given list of gene guids
        public bool HasGenes(ObservableCollection<string> gene_guids)
        {
            bool res = true;
            foreach (string guid in gene_guids)
            {
                if (!HasGene(guid))
                {
                    res = false;
                    break;
                }
            }
            return res;
        }

        /// <summary>
        /// This method looks for duplicate names with newly created (or copied) cell
        /// If it is a duplicate, a suffix like "_Copy" is added
        /// </summary>
        /// <param name="sc"></param>
        public void ValidateName(Protocol protocol)
        {
            bool found = false;
            string newCellName = CellName;
            foreach (ConfigCell cell in protocol.entity_repository.cells)
            {
                if (cell.CellName == newCellName && cell.entity_guid != entity_guid)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                CellName = GenerateNewName(protocol, "_Copy");
            }
        }

        public override string GenerateNewName(Level level, string ending)
        {
            string OriginalName = CellName;

            if (OriginalName.Contains(ending))
            {
                int index = OriginalName.IndexOf(ending);
                OriginalName = OriginalName.Substring(0, index);
            }

            int nSuffix = 1;
            string suffix = ending + string.Format("{0:000}", nSuffix);
            string TempCellName = OriginalName + suffix;
            while (FindCellByName(level, TempCellName) == true)
            {
                nSuffix++;
                suffix = ending + string.Format("{0:000}", nSuffix);
                TempCellName = OriginalName + suffix;
            }

            return TempCellName;
        }

        public static bool FindCellByName(Protocol protocol, string cellName)
        {
            bool ret = false;
            foreach (ConfigCell cell in protocol.entity_repository.cells)
            {
                if (cell.CellName == cellName)
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public static bool FindCellByName(Level level, string cellName)
        {
            bool ret = false;
            foreach (ConfigCell cell in level.entity_repository.cells)
            {
                if (cell.CellName == cellName)
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public ConfigGene FindGene(string guid)
        {
            foreach (ConfigGene g in genes)
            {
                if (g.entity_guid == guid)
                    return g;
            }

            return null;
        }

        public bool Equals(ConfigCell cc)
        {
            if (this.entity_guid != cc.entity_guid)
                return false;

            if (this.CellName != cc.CellName)
                return false;

            if (this.CellRadius != cc.CellRadius)
                return false;

            if (this.DragCoefficient != cc.DragCoefficient)
                return false;

            if (this.Sigma != cc.Sigma)
                return false;

            if (this.TransductionConstant != cc.TransductionConstant)
                return false;

            if (this.locomotor_mol_guid_ref != cc.locomotor_mol_guid_ref)
                return false;

            //Check cell genes
            foreach (ConfigGene gene in genes)
            {
                if (cc.HasGene(gene.entity_guid) == false)
                    return false;
                else
                {
                    ConfigGene gene2 = cc.FindGene(gene.entity_guid);
                    if (gene.Equals(gene2) == false)
                    {
                        return false;
                    }
                }
            }

            //Check cytosol molecules
            foreach (ConfigMolecule mol in cytosol.molecules_dict.Values)
            {
                bool bFound = cc.cytosol.HasMolecule(mol);
                if (bFound)
                {
                    ConfigMolecule mol2 = cc.cytosol.molecules_dict[mol.entity_guid];
                    if (mol.Equals(mol2) == false)
                        return false;
                }
            }

            //Check membrane molecules
            foreach (ConfigMolecule mol in membrane.molecules_dict.Values)
            {
                bool bFound = cc.membrane.HasMolecule(mol);
                if (bFound)
                {
                    ConfigMolecule mol2 = cc.membrane.molecules_dict[mol.entity_guid];
                    if (mol.Equals(mol2) == false)
                        return false;
                }
            }

            //Check cytosol reactions
            foreach (ConfigReaction reac in cytosol.Reactions)
            {
                if (cc.cytosol.reactions_dict.ContainsKey(reac.entity_guid) == false)
                {
                    return false;
                }
                else
                {
                    ConfigReaction reac2 = cc.cytosol.reactions_dict[reac.entity_guid];
                    if (reac.Equals(reac2) == false)
                        return false;
                }
            }

            //Check membrane reactions
            foreach (ConfigReaction reac in membrane.Reactions)
            {
                if (cc.membrane.reactions_dict.ContainsKey(reac.entity_guid) == false)
                {
                    return false;
                }
                else
                {
                    ConfigReaction reac2 = cc.membrane.reactions_dict[reac.entity_guid];
                    if (reac.Equals(reac2) == false)
                        return false;
                }
            }

            //Check diff scheme
            if (this.diff_scheme == null && cc.diff_scheme != null)
                return false;
            else if (this.diff_scheme != null && cc.diff_scheme == null)
                return false;
            else if (this.diff_scheme == null && cc.diff_scheme == null)
            {
            }
            else if (this.diff_scheme.Equals(cc.diff_scheme) == false)
                return false;

            //Check div scheme
            if (this.div_scheme == null && cc.div_scheme != null)
                return false;
            else if (this.div_scheme != null && cc.div_scheme == null)
                return false;
            else if (this.div_scheme == null && cc.div_scheme == null)
            {
            }
            else if (this.div_scheme.Equals(cc.div_scheme) == false)
                return false;

            //Check death driver
            if (this.death_driver == null && cc.death_driver != null)
                return false;
            else if (this.death_driver != null && cc.death_driver == null)
                return false;
            else if (this.death_driver == null && cc.death_driver == null)
            {
            }
            else if (this.death_driver.Equals(cc.death_driver) == false)
                return false;

            return true;
        }

        /// <summary>
        /// Force distributed parameters to reinitialize on the next Sample.
        /// This is needed in order to get reproducible results for the same global seed value.
        /// </summary>
        public void ResetDistributedParameters()
        {
                TransductionConstant.Reset();
                Sigma.Reset();
                DragCoefficient.Reset();
                if (death_driver != null)
                {
                    death_driver.CurrentState.Reset();
                }
                if (diff_scheme != null)
                {
                    diff_scheme.Driver.CurrentState.Reset();
                }
                if (div_scheme != null)
                {
                    div_scheme.Driver.CurrentState.Reset();
                }
        }
    }

    public enum CellPopDistributionType { Specific, Uniform, Gaussian }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(CellPopDistributionType), typeof(string))]
    public class CellPopDistributionTypeToStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> _cell_pop_dist_type_strings = new List<string>()
            {
                "Specify cell coordinates",
                "Uniform",
                "Gaussian"
            };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value as string == "") return value;
            try
            {
                int n = (int)value;
                return _cell_pop_dist_type_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _cell_pop_dist_type_strings.FindIndex(item => item == str);
            return (CellPopDistributionType)Enum.ToObject(typeof(CellPopDistributionType), (int)idx);
        }
    }

    public interface ProbDistribution3D
    {
        /// <summary>
        /// Return x,y,z coordinates for the next cell using the appropriate probability density distribution.
        /// </summary>
        /// <returns>double[3] {x,y,z}</returns>
        double[] nextPosition();
        /// <summary>
        /// Update extents of ECS and other distribution-specific tasks.
        /// </summary>
        /// <param name="newExtents"></param>
        void Resize(double[] newExtents);
    }

    /// <summary>
    /// Contains information for positioning cells in the ECS according to specfied distributions.
    /// </summary>
    public abstract class CellPopDistribution : EntityModelBase, ProbDistribution3D
    {
        private CellPopDistributionType _DistType;
        public CellPopDistributionType DistType
        {
            get { return _DistType; }
            set
            {
                if (_DistType == value)
                    return;
                else
                {
                    _DistType = value;
                    OnPropertyChanged("DistType");
                }
            }
        }

        // We need to update (reduce) cellPop.number if we reach the maximum tries 
        // for cell placement before all the cells are placed
        public CellPopulation cellPop;

        // Limits for placing cells
        protected double[] extents;
        public double[] Extents
        {
            get { return extents; }
            set { extents = value; }
        }
        // Separation distance from boundaries
        private double wallDis;
        // Minimum separation (squared) for cells
        private double minDisSquared;
        public double MinDisSquared
        {
            get { return minDisSquared; }
            set
            {
                minDisSquared = value;
                //// Generally, minDisSquared is the square of the cell diameter.
                //// Then, center of cell can be one radius from the ECS boundary.
                // wallDis = Math.Sqrt(minDisSquared) / 2.0;
                wallDis = 0;
            }
        }

        public CellPopDistribution(double[] _extents, double _minDisSquared, CellPopulation _cellPop)
        {
            //cellPop.CellStates = new ObservableCollection<CellState>();
            extents = (double[])_extents.Clone();
            MinDisSquared = _minDisSquared;

            // null case when deserializing Json
            // correct CellPopulation pointer added in Protocoluration.InitCellPopulationIDCellPopulationDict
            if (_cellPop != null)
            {
                cellPop = _cellPop;
            }
        }

        /// <summary>
        /// Check that the new cell position is within the specified bounds.
        /// </summary>
        /// <param name="pos">the position of the next cell</param>
        /// <returns></returns>
        protected bool inBounds(double[] pos)
        {
            if ((pos[0] < wallDis || pos[0] > Extents[0] - wallDis) ||
                (pos[1] < wallDis || pos[1] > Extents[1] - wallDis) ||
                (pos[2] < wallDis || pos[2] > Extents[2] - wallDis))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Initialize the cell states
        /// </summary>
        /// <param name="extents"></param>
        /// <param name="box"></param>
        public void Initialize()
        {
            if (cellPop != null)
            {
                cellPop.CellStates.Clear();
                AddByDistr(cellPop.number);
            }

        }

        /// <summary>
        /// Return true if the position of the new cell doesn't overlap with existing cell positions.
        /// NOTE: We should be checking for overlap with all cell populations. Not sure how to do this, yet.
        /// </summary>
        /// <param name="pos">the position of the next cell</param>
        /// <returns></returns>
        protected bool noOverlap(double[] pos)
        {
            double disSquared = 0;
            foreach (CellState cellState in this.cellPop.CellStates)
            {
                disSquared = (cellState.X - pos[0]) * (cellState.X - pos[0])
                           + (cellState.Y - pos[1]) * (cellState.Y - pos[1])
                           + (cellState.Z - pos[2]) * (cellState.Z - pos[2]);
                if (disSquared < minDisSquared)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check that position is in-bounds and doesn't overlap.
        /// If so, add to cell location list.
        /// </summary>
        /// <param name="pos">x,y,z coordinates</param>
        /// <returns></returns>
        public bool AddByPosition(double[] pos)
        {
            if (inBounds(pos) && noOverlap(pos))
            {
                cellPop.CellStates.Add(new CellState(pos[0], pos[1], pos[2]));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add n cells using the appropriate probability density distribution.
        /// </summary>
        /// <param name="n"></param>
        public void AddByDistr(int n)
        {
            // NOTE: The maxTry settings has been arbitrarily chosen and may need to be adjusted.
            int maxTry = 1000;
            int i = 0;

            int tries = 0;
            while (i < n)
            {

                if (AddByPosition(nextPosition()))
                {
                    i++;
                    tries = 0;
                }
                else
                {
                    // Avoid infinite loops. Excessive iterations may indicate the cells density is too high.
                    tries++;
                    if (tries > maxTry)
                    {
                        if (cellPop.CellStates.Count < 1)
                        {
                            AddByPosition(new double[] { Extents[0] / 2.0, Extents[1] / 2.0, Extents[2] / 2.0 });
                        }
                        System.Windows.MessageBox.Show("Exceeded max iterations for cell placement. Cell density is too high. Limiting cell count to " + cellPop.CellStates.Count + ".");
                        cellPop.number = cellPop.CellStates.Count;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Return x,y,z coordinates for the next cell using the appropriate probability density distribution.
        /// </summary>
        /// <returns>double[3] {x,y,z}</returns>
        public abstract double[] nextPosition();

        // Clear the current cell states and repopulate
        // Needed if Gaussian/Box parameters change.
        public void Reset()
        {
            int number = cellPop.CellStates.Count;
            cellPop.CellStates.Clear();
            AddByDistr(number);
        }

        // <summary>
        // Triggered by OnUpdate of ConfigEnvironment
        // Update distributions accordingly.
        // </summary>
        // <param name="newextents"></param>
        public abstract void Resize(double[] newExtents);

        /// <summary>
        /// Check that all cells are in-bounds. 
        /// </summary>
        public void CheckPositions()
        {
            double[] pos;
            int number = cellPop.CellStates.Count;

            // Remove out-of-bounds cells
            for (int i = cellPop.CellStates.Count - 1; i >= 0; i--)
            {
                pos = new double[3] { cellPop.CellStates[i].X, cellPop.CellStates[i].Y, cellPop.CellStates[i].Z };
                if (!inBounds(pos))
                {
                    cellPop.CellStates.RemoveAt(i);
                }
            }

            // Replace removed cells
            int cellsToAdd = number - cellPop.CellStates.Count;
            if (cellsToAdd > 0)
            {
                AddByDistr(cellsToAdd);
            }
        }
    }

    /// <summary>
    /// Uses uniform probability density for initial placement of cells. 
    /// NOTE: It may make more sense to have this be the (non-abstract) base class.
    /// </summary>
    public class CellPopSpecific : CellPopDistribution
    {
        public CellPopSpecific(double[] extents, double minDisSquared, CellPopulation _cellPop)
            : base(extents, minDisSquared, _cellPop)
        {
            DistType = CellPopDistributionType.Specific;
        }

        public override double[] nextPosition()
        {
            return new double[3] {  Extents[0] * Rand.UniformDist.Sample(), 
                                    Extents[1] * Rand.UniformDist.Sample(), 
                                    Extents[2] * Rand.UniformDist.Sample() };
        }

        public override void Resize(double[] newExtents)
        {
            // Check whether extents changed
            if ((Extents[0] != newExtents[0]) || (Extents[1] != newExtents[1]) || (Extents[2] != newExtents[2]))
            {
                Extents = (double[])newExtents.Clone();
                CheckPositions();
            }
        }
    }

    /// <summary>
    /// Placement of cells via uniform probability density.
    /// </summary>
    public class CellPopUniform : CellPopDistribution
    {
        public CellPopUniform(double[] extents, double minDisSquared, CellPopulation _cellPop)
            : base(extents, minDisSquared, _cellPop)
        {
            DistType = CellPopDistributionType.Uniform;
        }

        public override double[] nextPosition()
        {
            return new double[3] {  Extents[0] * Rand.UniformDist.Sample(), 
                                    Extents[1] * Rand.UniformDist.Sample(), 
                                    Extents[2] * Rand.UniformDist.Sample() };
        }

        public override void Resize(double[] newExtents)
        {
            // Check whether extents changed
            if ((Extents[0] != newExtents[0]) || (Extents[1] != newExtents[1]) || (Extents[2] != newExtents[2]))
            {
                Extents = (double[])newExtents.Clone();
                Reset();
            }
        }
    }

    /// <summary>
    /// Placement of cells via normal probability density.
    /// Cell placement updates as box center and width changes.
    /// </summary>
    public class CellPopGaussian : CellPopDistribution
    {
        public GaussianSpecification gauss_spec { get; set; }
        // The standard deviations of the distribution
        private double[] sigma;

        // transformation matrix for converting from absolute (simulation) 
        // to local (box) coordinates
        private double[][] ATL = new double[][] {   new double[]{1.0, 0.0, 0.0, 0.0},
                                                    new double[]{0.0, 1.0, 0.0, 0.0},
                                                    new double[]{0.0, 0.0, 1.0, 0.0},
                                                    new double[]{0.0, 0.0, 0.0, 1.0} };

        public CellPopGaussian(double[] extents, double minDisSquared, CellPopulation _cellPop)
            : base(extents, minDisSquared, _cellPop)
        {
            DistType = CellPopDistributionType.Gaussian;
        }

        /// <summary>
        /// set the box handler, sigma, and rotation matrix
        /// </summary>
        /// <param name="extents"></param>
        /// <param name="box"></param>
        public void InitializeGaussSpec(GaussianSpecification _gaussSpec)
        {
            gauss_spec = _gaussSpec;

            if (gauss_spec != null)
            {
                if (gauss_spec.box_spec != null)
                {
                    gauss_spec.box_spec.PropertyChanged += new PropertyChangedEventHandler(CellPopGaussChanged);
                    sigma = new double[3] { gauss_spec.box_spec.x_scale / 2, gauss_spec.box_spec.y_scale / 2, gauss_spec.box_spec.z_scale / 2 };
                    setRotationMatrix(gauss_spec.box_spec);
                }
            }
            else
            {
                sigma = new double[3] { extents[0] / 4, extents[1] / 4, extents[2] / 4 };
            }
        }

        public override void Resize(double[] newExtents)
        {
            // Check whether extents changed
            if ((Extents[0] != newExtents[0]) || (Extents[1] != newExtents[1]) || (Extents[2] != newExtents[2]))
            {
                Extents = (double[])newExtents.Clone();
                Reset();
            }
        }

        public void ParamReset(BoxSpecification box)
        {
            sigma = new double[3] { box.x_scale / 2, box.y_scale / 2, box.z_scale / 2 };
            setRotationMatrix(box);
        }

        private void setRotationMatrix(BoxSpecification box)
        {
            // 4x4 transformation matrix comprising:
            //      normalized 3x3 rotation matrix
            //      translation information
            for (int i = 0; i < 3; i++)
            {
                ATL[i][0] = box.transform_matrix[i][0] / box.getScale((byte)0);
                ATL[i][1] = box.transform_matrix[i][1] / box.getScale((byte)1);
                ATL[i][2] = box.transform_matrix[i][2] / box.getScale((byte)2);
                ATL[i][3] = box.transform_matrix[i][3];
            }
        }

        public void CellPopGaussChanged(object sender, PropertyChangedEventArgs e)
        {
            BoxSpecification box = (BoxSpecification)sender;
            ParamReset(box);
            Reset();
        }

        public override double[] nextPosition()
        {
            // Draw three random coordinates from normal distributions centered at the origin of the simulation coordinate system.
            // normal distribution centered at zero with specified sigmas
            double[] pos = new double[3] {  sigma[0] * Rand.NormalDist.Sample(), 
                                            sigma[1] * Rand.NormalDist.Sample(), 
                                            sigma[2] * Rand.NormalDist.Sample() };

            // The new position rotated and translated  with the box coordinate system
            double[] posRotated = new double[3];
            // rotates and translates center of distribution
            for (int i = 0; i < 3; i++)
            {
                posRotated[i] = pos[0] * ATL[i][0] +
                                pos[1] * ATL[i][1] +
                                pos[2] * ATL[i][2] +
                                ATL[i][3];
            }
            return posRotated;
        }
    }

    public class CellMolPopState
    {
        public Dictionary<String, double[]> molPopDict { get; set; }
        public CellMolPopState()
        {
            molPopDict = new Dictionary<string, double[]>();
        }
    }

    public class CellBehaviorState
    {
        //saving current state of each driver
        public int deathDriveState;
        public int divisionDriverState;
        public int differentiationDriverState;

        public CellBehaviorState()
        {
            deathDriveState = -1;
            divisionDriverState = -1;
            differentiationDriverState = -1;
        }
    }

    public class CellGeneState
    {
        //double to save gene’s activity
        public Dictionary<String, double> geneDict { get; set; }
        public CellGeneState()
        {
            geneDict = new Dictionary<string, double>();
        }
    }

    public class CellState
    {
        public CellSpatialState spState;
        public CellMolPopState cmState;
        public CellBehaviorState cbState;
        public CellGeneState cgState;
        public int CellGeneration;

        [JsonIgnore]
        public double X
        {
            get { return Math.Round(spState.X[0], 2); }
            set { spState.X[0] = value; }
        }

        [JsonIgnore]
        public double Y
        {
            get { return Math.Round(spState.X[1], 2); }
            set { spState.X[1] = value; }
        }

        [JsonIgnore]
        public double Z
        {
            get { return Math.Round(spState.X[2], 2); }
            set { spState.X[2] = value; }
        }

        public CellState()
        {
            spState.X = new double[3];
            spState.V = new double[3];
            spState.F = new double[3];

            cmState = new CellMolPopState();
            cbState = new CellBehaviorState();
            cgState = new CellGeneState();
        }

        public CellState(double x, double y, double z)
        {
            spState.X = new double[3] { x, y, z };
            spState.V = new double[3];
            spState.F = new double[3];
            cmState = new CellMolPopState();
            cbState = new CellBehaviorState();
            cgState = new CellGeneState();
        }

        public void setSpatialState(CellSpatialState state)
        {
            Array.Copy(state.X, spState.X, 3);
            Array.Copy(state.V, spState.V, 3);
            Array.Copy(state.F, spState.F, 3);
        }

        public void addMolPopulation(string key, MolecularPopulation mp)
        {
            cmState.molPopDict.Add(key, mp.CopyArray());
        }

        public void setDeathDriverState(int state)
        {
            cbState.deathDriveState = state;
        }

        public void setDivisonDriverState(int state)
        {
            cbState.deathDriveState = state;
        }

        public void setDifferentiationDriverState(int state)
        {
            cbState.differentiationDriverState = state;
        }

        public void setGeneState(Dictionary<string, Gene> genes)
        {
            foreach (var item in genes)
            {
                cgState.geneDict.Add(item.Key, item.Value.ActivationLevel);
            }
        }

        public void setCellGeneration(int generation)
        {
            CellGeneration = generation;
        }

    }

    public class ReportXVF
    {
        public bool position { get; set; }
        public bool velocity { get; set; }
        public bool force { get; set; }

        public ReportXVF()
        {
            position = false;
            velocity = false;
            force = false;
        }
    }

    public class ReportStates
    {
        public bool Death { get; set; }
        public bool Division { get; set; }
        public bool Differentiation { get; set; }
    }

    public class CellPopulation : EntityModelBase
    {
        //public string cell_guid_ref { get; set; }
        private ConfigCell _Cell;

        public ConfigCell Cell
        {
            get
            {
                return _Cell;
            }
            set
            {
                _Cell = value;
                if (_Cell != null)
                {
                    renderLabel = _Cell.renderLabel ?? _Cell.entity_guid;
                }
                OnPropertyChanged("Cell");
            }
        }

        private string _Name;
        public string cellpopulation_name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                OnPropertyChanged("cellpopulation_name");
            }
        }
        public string cellpopulation_guid { get; set; }
        public int cellpopulation_id { get; set; }

        private ReportXVF reportXVF;
        public ReportXVF report_xvf
        {
            get { return reportXVF; }
            set
            {
                reportXVF = value;
            }
        }

        private ReportStates report_states;
        public ReportStates reportStates
        {
            get
            {
                return report_states;
            }
            set
            {
                report_states = value;
            }
        }

        private ObservableCollection<ReportECM> ecmProbe;
        public ObservableCollection<ReportECM> ecm_probe
        {
            get { return ecmProbe; }
        }

        [JsonIgnore]
        public Dictionary<string, ReportECM> ecm_probe_dict;

        private int _number;
        public int number
        {
            get { return _number; }
            set
            {
                if (_number == value)
                    return;
                else
                {
                    _number = value;
                    OnPropertyChanged("number");
                }
            }
        }

        private ObservableCollection<CellState> cellStates;
        public ObservableCollection<CellState> CellStates
        {
            get { return cellStates; }
            set
            {
                cellStates = value;
                OnPropertyChanged("CellStates");
            }
        }

        /// <summary>
        /// Remove n cells from the end of the list
        /// </summary>
        /// <param name="num"></param>
        public void RemoveCells(int num)
        {
            int i = 0;
            while ((i < num) && (cellStates.Count > 0))
            {
                cellStates.RemoveAt(cellStates.Count - 1);
                i++;
            }
        }


        private CellPopDistribution _cellPopDist;
        public CellPopDistribution cellPopDist
        {
            get { return _cellPopDist; }
            set
            {
                if (_cellPopDist == value)
                    return;
                else
                {
                    _cellPopDist = value;
                    OnPropertyChanged("cellPopDist");
                }
            }
        }

        public string renderLabel { get; set; }        //label to color scheme

        public CellPopulation()
        {
            Guid id = Guid.NewGuid();

            cellpopulation_guid = id.ToString();
            cellpopulation_name = "";
            number = 1;
            cellpopulation_id = Protocol.SafeCellPopulationID++;
            // reporting
            reportXVF = new ReportXVF();
            reportStates = new ReportStates();
            ecmProbe = new ObservableCollection<ReportECM>();
            ecm_probe_dict = new Dictionary<string, ReportECM>();
            cellStates = new ObservableCollection<CellState>();

            renderLabel = cellpopulation_guid;
        }
    }

    /// <summary>
    /// Converter to go between molecule GUID references in MolPops
    /// and molecule names kept in the repository of molecules.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class MolGUIDtoMolNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string mol_name = "";

            if (parameter == null || guid == "")
                return mol_name;

            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigMolecule> mol_list = cvs.Source as ObservableCollection<ConfigMolecule>;
            if (mol_list != null)
            {
                foreach (ConfigMolecule mol in mol_list)
                {
                    if (mol.entity_guid == guid)
                    {
                        mol_name = mol.Name;
                        break;
                    }
                }
            }
            return mol_name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between molecule GUID references in MolPops
    /// and molecule names kept in the repository of molecules.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class MolPopGUIDtoMolPopNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter == null)
                return "";

            string guid = value as string;
            string molpop_name = "";
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigMolecularPopulation> molpop_list = cvs.Source as ObservableCollection<ConfigMolecularPopulation>;
            if (molpop_list != null)
            {
                foreach (ConfigMolecularPopulation mp in molpop_list)
                {
                    if (mp.molpop_guid == guid)
                    {
                        molpop_name = mp.Name;
                        break;
                    }
                }
            }
            return molpop_name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between gene GUID references in cytosol
    /// and gene names kept in the repository of genes.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class GeneGUIDtoNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string gene_name = "";
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigGene> gene_list = cvs.Source as ObservableCollection<ConfigGene>;
            if (gene_list != null)
            {
                foreach (ConfigGene gene in gene_list)
                {
                    if (gene.entity_guid == guid)
                    {
                        gene_name = gene.Name;
                        break;
                    }
                }
            }
            return gene_name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between gene GUID references in cytosol
    /// and ConfigGene kept in the repository of genes.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class GeneGUIDtoConfigGeneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;

            if (guid == "")
                return null;

            ConfigGene thisGene = null;
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigGene> gene_list = cvs.Source as ObservableCollection<ConfigGene>;
            if (gene_list != null)
            {
                foreach (ConfigGene gene in gene_list)
                {
                    if (gene.entity_guid == guid)
                    {
                        thisGene = gene;
                        break;
                    }
                }
            }
            return thisGene;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    public class MolGuidToMolPopForDiffConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string driver_mol_guid = value as string;
            ConfigCompartment cc = parameter as ConfigCompartment;
            ConfigMolecularPopulation MyMolPop = null;

            if (driver_mol_guid == "" || cc == null)
                return MyMolPop;

            foreach (ConfigMolecularPopulation molpop in cc.molpops)
            {
                if (molpop.molecule.entity_guid == driver_mol_guid)
                {
                    MyMolPop = molpop;
                    break;
                }
            }

            return MyMolPop;

        }


        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConfigMolecularPopulation molpop = value as ConfigMolecularPopulation;

            if (molpop != null)
            {
                return molpop.molecule.entity_guid;
            }

            return "";
        }

    }

    public class MolGuidToMolPopForDiffMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null || values.Length < 2) return null;
            string driver_mol_guid = values[0] as string;
            ConfigCompartment cc = values[1] as ConfigCompartment;
            ConfigMolecularPopulation MyMolPop = null;

            if (driver_mol_guid == "" || cc == null)
                return MyMolPop;

            foreach (ConfigMolecularPopulation molpop in cc.molpops)
            {
                if (molpop.molecule.entity_guid == driver_mol_guid)
                {
                    MyMolPop = molpop;
                    break;
                }
            }

            return MyMolPop;

        }


        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            ConfigMolecularPopulation molpop = value as ConfigMolecularPopulation;

            if (molpop != null && molpop.molecule != null)
            {
                return new object[] { molpop.molecule.entity_guid };
            }

            return new object[] { "" };
        }

    }


    public class DriverElementToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string driver_mol_guid = value as string;
            bool enabled = true;

            if (driver_mol_guid == "")
                enabled = false;

            return enabled;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

    }

    /// <summary>
    /// Converter to go between molecule GUID references in MolPops
    /// and molecule names kept in the repository of molecules.
    /// </summary>
    [ValueConversion(typeof(string), typeof(ObservableCollection<ConfigMolecularPopulation>))]
    public class CellGUIDtoCellNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string cell_name = "";
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigCell> cell_list = cvs.Source as ObservableCollection<ConfigCell>;
            if (cell_list != null)
            {
                foreach (ConfigCell cel in cell_list)
                {
                    if (cel.entity_guid == guid)
                    {
                        cell_name = cel.CellName;
                        break;
                    }
                }
            }
            return cell_name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between cell pop and cell membrane MolPops
    /// </summary>
    [ValueConversion(typeof(string), typeof(ObservableCollection<ConfigMolecularPopulation>))]
    public class CellGuidToCellMembMolPopsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            string guid = value as string;
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigCell> cell_list = cvs.Source as ObservableCollection<ConfigCell>;
            if (cell_list != null)
            {
                foreach (ConfigCell cel in cell_list)
                {
                    if (cel.entity_guid == guid)
                    {
                        return cel.membrane.molpops;
                    }
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between cell pop and cell cytosol MolPops
    /// </summary>
    [ValueConversion(typeof(string), typeof(ObservableCollection<ConfigMolecularPopulation>))]
    public class CellGuidToCellCytoMolPopsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            string guid = value as string;
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigCell> cell_list = cvs.Source as ObservableCollection<ConfigCell>;
            if (cell_list != null)
            {
                foreach (ConfigCell cel in cell_list)
                {
                    if (cel.entity_guid == guid)
                    {
                        return cel.cytosol.molpops;
                    }
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between molecule GUID references in MolPops
    /// and molecule names kept in the repository of molecules.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class ReactionGUIDtoReactionStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string reac_string = "";
            //string cult = culture as string;

            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigReaction> reac_list = cvs.Source as ObservableCollection<ConfigReaction>;
            if (reac_list != null)
            {
                foreach (ConfigReaction cr in reac_list)
                {
                    if (cr.entity_guid == guid)
                    {
                        //This next if is a complete hack!
                        if (culture.Name == "en-US")
                            reac_string = cr.TotalReactionString;
                        else
                            reac_string = cr.rate_const.ToString("G5", System.Globalization.CultureInfo.InvariantCulture); //ToString("E3");  //("#.##E0");        //("#.00");
                        break;
                    }
                }
            }
            return reac_string;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between molecule GUID references in MolPops
    /// and molecule names kept in the repository of molecules.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class ReactionComplexGUIDtoReactionComplexStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string rc_string = "";

            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigReactionComplex> rc_list = cvs.Source as ObservableCollection<ConfigReactionComplex>;
            if (rc_list != null)
            {
                foreach (ConfigReactionComplex crc in rc_list)
                {
                    if (crc.entity_guid == guid)
                    {
                        //This next if is a complete hack!
                        rc_string = crc.Name;
                        break;
                    }
                }
            }
            return rc_string;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    //ReacComplexGUIDtoReactionStringsConverter
    /// <summary>
    /// Converter to go between molecule GUID references in MolPops
    /// and molecule names kept in the repository of molecules.
    /// </summary>
    [ValueConversion(typeof(string), typeof(ConfigReactionComplex))]
    public class ReacComplexGUIDtoReactionComplexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;

            if (guid == "")
                return null;

            ConfigReactionComplex rcReturn = null;
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigReactionComplex> rc_list = cvs.Source as ObservableCollection<ConfigReactionComplex>;
            if (rc_list != null)
            {
                foreach (ConfigReactionComplex crc in rc_list)
                {
                    if (crc.entity_guid == guid)
                    {
                        rcReturn = crc;
                        break;
                    }
                }
            }
            return rcReturn;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Convert System.Windows.Media.Color to SolidBrush for rectangle fills
    /// </summary>
    public class SWMColorToSolidBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            System.Windows.Media.Color color;

            try
            {
                color = (System.Windows.Media.Color)value;
            }
            catch
            {
                color = System.Windows.Media.Color.FromRgb(0, 0, 0);
            }
            return new System.Windows.Media.SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    //ColorListToBrushConverter
    /// <summary>
    /// Convert ColorList enum to SolidBrush for rectangle fills
    /// </summary>
    public class ColorListToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Color col = Color.FromRgb(255, 0, 0);
            if (value == null || value as string == "")
                return col;

            try
            {
                int index = (int)value;
                ColorList colEnum = (ColorList)Enum.ToObject(typeof(ColorList), (int)index);

                switch (colEnum)
                {
                    case ColorList.Red:
                        col = Colors.Red;
                        break;
                    case ColorList.Orange:
                        col = Colors.Orange;
                        break;
                    case ColorList.Yellow:
                        col = Colors.Yellow;
                        break;
                    case ColorList.Green:
                        col = Colors.Green;
                        break;
                    case ColorList.Blue:
                        col = Colors.Blue;
                        break;
                    case ColorList.Indigo:
                        col = Colors.Indigo;
                        break;
                    case ColorList.Violet:
                        col = Colors.Violet;
                        break;
                    case ColorList.Custom:
                    default:
                        break;
                }

                return new System.Windows.Media.SolidColorBrush(col);
            }
            catch
            {
                return new System.Windows.Media.SolidColorBrush(col);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 0;
        }
    }

    //public enum MolPopDistributionType { Homogeneous, Linear, Gaussian, Custom, Explicit }
    public enum MolPopDistributionType { Homogeneous, Linear, Gaussian, Explicit }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(MolPopDistributionType), typeof(string))]
    public class MolPopDistributionTypeToStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> _molpop_dist_type_strings = new List<string>()
                                {
                                    "Homogeneous",
                                    "Linear",
                                    "Gaussian",
                                    //"Custom",
                                    "Explicit",
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value as string == "") return "";
            try
            {
                return _molpop_dist_type_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _molpop_dist_type_strings.FindIndex(item => item == str);
            return (MolPopDistributionType)Enum.ToObject(typeof(MolPopDistributionType), (int)idx);
        }
    }

    // Base class for homog, linear, gauss distributions
    [XmlInclude(typeof(MolPopHomogeneousLevel)),
     XmlInclude(typeof(MolPopLinear)),
     XmlInclude(typeof(MolPopGaussian))]
    public abstract class MolPopDistribution : EntityModelBase, IEquatable<MolPopDistribution>
    {
        public MolPopDistributionType mp_distribution_type { get; protected set; }
        public List<BoundaryCondition> boundaryCondition { get; set; }

        public MolPopDistribution()
        {
        }

        public abstract bool Equals(MolPopDistribution mpd);        

    }

    public class MolPopHomogeneousLevel : MolPopDistribution
    {
        private double _concentration;
        public double concentration
        {
            get
            {
                return _concentration;
            }
            set
            {
                _concentration = value;
                OnPropertyChanged("concentration");
            }
        }

        public MolPopHomogeneousLevel()
        {
            mp_distribution_type = MolPopDistributionType.Homogeneous;
            concentration = 1.0;
        }

        public override bool Equals(MolPopDistribution mph)
        {

            if (this.concentration != (mph as MolPopHomogeneousLevel).concentration)
                return false;

            return true;
        }
    }

    public class MolPopLinear : MolPopDistribution
    {
        public double x1 { get; set; }
        public int dim { get; set; }
        private BoundaryFace _boundary_face;
        public BoundaryFace boundary_face
        {
            get
            {
                return _boundary_face;
            }
            set
            {
                _boundary_face = value;
                dim = (int)_boundary_face - 1;
            }
        }

        public MolPopLinear()
        {
            mp_distribution_type = MolPopDistributionType.Linear;
            x1 = 0;
            boundaryCondition = new List<BoundaryCondition>();
        }

        public void Initalize(BoundaryFace b_face)
        {
            boundary_face = b_face;
            dim = (int)boundary_face - 1;

            switch (boundary_face)
            {
                case BoundaryFace.X:
                    boundaryCondition[0].boundary = Boundary.left;
                    boundaryCondition[1].boundary = Boundary.right;
                    break;

                case BoundaryFace.Y:
                    boundaryCondition[0].boundary = Boundary.bottom;
                    boundaryCondition[1].boundary = Boundary.top;
                    break;

                case BoundaryFace.Z:
                    boundaryCondition[0].boundary = Boundary.back;
                    boundaryCondition[1].boundary = Boundary.front;
                    break;
                case BoundaryFace.None:
                    break;

            }
        }

        public override bool Equals(MolPopDistribution mpd)
        {
            MolPopLinear mpl = mpd as MolPopLinear;

            if (this.x1 != mpl.x1 || this.dim != mpl.dim)
                return false;

            if (this.boundary_face != mpl.boundary_face)
                return false;

            return true;
        }
    }

    public class MolPopGaussian : MolPopDistribution
    {
        public double peak_concentration { get; set; }
        public GaussianSpecification gauss_spec { get; set; }

        public MolPopGaussian()
        {
            mp_distribution_type = MolPopDistributionType.Gaussian;
            peak_concentration = 1.0;
        }

        public override bool Equals(MolPopDistribution mpd)
        {
            MolPopGaussian mpg = mpd as MolPopGaussian;

            if (this.peak_concentration != mpg.peak_concentration)
                return false;

            if (this.gauss_spec.Equals(mpg.gauss_spec) == false)
                return false;

            return true;
        }
    }

    /// <summary>
    /// added to store intermediate run state
    /// </summary>
    public class MolPopExplicit : MolPopDistribution
    {
        public MolPopExplicit()
        {
            mp_distribution_type = MolPopDistributionType.Explicit;
            Description = "";
            MolFileName = "";
        }

        public void Initialize(int[] numGridPoints)
        {
            //create array of actual size, initialized to zeroes
            int totalExpectedValues = numGridPoints[0] * numGridPoints[1] * numGridPoints[2];
            conc = new double[totalExpectedValues];
        }

        public override bool Equals(MolPopDistribution mpd)
        {
            MolPopExplicit mpe = mpd as MolPopExplicit;

            if (this.MolFileName.Equals(mpe.MolFileName) == false)
                return false;

            if (this.Description.Equals(mpe.Description) == false)
                return false;

            if (this.conc.Length != mpe.conc.Length)
                return false;

            for (int i = 0; i < this.conc.Length; i++)
            {
                if (this.conc[i] != mpe.conc[i])
                    return false;
            }

            return true;
        }

        public double[] conc;
        public string Description { get; set; }

        private string molFileName;
        public string MolFileName
        {
            get
            {
                return molFileName;
            }
            set
            {
                if (molFileName != value)
                {
                    molFileName = value;
                    OnPropertyChanged("MolFileName");
                }
            }
        }

        public void Load(int[] numGridPoints)
        {
            if (MolFileName == null || MolFileName.Length == 0)
            {
                MessageBox.Show("File name not specified. \nAll molecular concentrations set to zero.", "File not specified", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (File.Exists(MolFileName) == false)
            {
                MessageBox.Show(string.Format("File not found:  {0}. \nAll molecular concentrations set to zero.", MolFileName), "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string readText = File.ReadAllText(MolFileName);
            if (readText.Length == 0)
            {
                MessageBox.Show(string.Format("Input file is empty:  {0}. \nAll molecular concentrations set to zero.", MolFileName), "Empty file", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            double[] readconcs;
            try
            {
                readconcs = readText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(s => double.Parse(s)).ToArray();
            }
            catch (FormatException e)
            {
                MessageBox.Show(string.Format("This file contains invalid data. \nAll molecular concentrations set to zero."),
                   "Invalid data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            catch (OverflowException ex)
            {
                MessageBox.Show(string.Format("This file contains a value that is out of range. \nAll molecular concentrations set to zero."),
                   "Data out of range", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //at this point, we have a file with valid double values
            //call input validator to check for other problems
            if (validateInput(numGridPoints, readconcs) == true)
            {
                //This means input values are valid so copy them            
                conc = readconcs;
                MessageBox.Show("File successfully loaded.", "Load succeeded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool validateInput(int[] numGridPoints, double[] readconcs)
        {
            int actualValuesInFile = readconcs.Length;
            int totalExpectedValues = numGridPoints[0] * numGridPoints[1] * numGridPoints[2];

            //Check for negative numbers
            if (readconcs.Where(s => s < 0).Any())
            {
                MessageBox.Show(string.Format("This file contains negative values. \nAll molecular concentrations set to zero."),
                    "Invalid number of points", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            //Check for invalid number of entries - if more entries exist than needed, then we just use as many values as provided
            else if (actualValuesInFile < totalExpectedValues)
            {
                MessageBox.Show(string.Format("This file contains {0} values. The number of expected values is: {1}. \nAll molecular concentrations set to zero.", actualValuesInFile, totalExpectedValues),
                    "Invalid number of points", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            //OK
            else
            {
                return true;
            }

        }
    }

    public class GaussianSpecification : EntityModelBase
    {
        // gmk - these aren't used. phase out. 
        [JsonIgnore]
        private string _gaussian_spec_name = "";
        [JsonIgnore]
        public string gaussian_spec_name
        {
            get { return _gaussian_spec_name; }
            set
            {
                if (_gaussian_spec_name == value)
                    return;
                else
                {
                    _gaussian_spec_name = value;
                    OnPropertyChanged("gaussian_spec_name");
                }
            }
        }
        public BoxSpecification box_spec { get; set; }

        private bool _current_gaussian_region_visibility = true;
        public bool current_gaussian_region_visibility
        {
            get { return _current_gaussian_region_visibility; }
            set
            {
                if (_current_gaussian_region_visibility == value)
                    return;
                else
                {
                    _current_gaussian_region_visibility = value;
                    OnPropertyChanged("current_gaussian_region_visibility");
                }
            }
        }

        private bool _gaussian_region_visibity = true;
        public bool gaussian_region_visibility
        {
            get { return _gaussian_region_visibity; }
            set
            {
                if (_gaussian_region_visibity == value)
                    return;
                else
                {
                    _gaussian_region_visibity = value;
                    OnPropertyChanged("gaussian_region_visibility");
                }
            }
        }
        private System.Windows.Media.Color _gaussian_spec_color;
        public System.Windows.Media.Color gaussian_spec_color
        {
            get { return _gaussian_spec_color; }
            set
            {
                if (_gaussian_spec_color == value)
                    return;
                else
                {
                    _gaussian_spec_color = value;
                    OnPropertyChanged("gaussian_spec_color");
                }
            }
        }

        ////Add this after 2/4/14
        ////public bool DrawAsWireframe { get; set; }

        public GaussianSpecification()
        {
            gaussian_spec_name = "";
            gaussian_region_visibility = true;
            current_gaussian_region_visibility = true;
            gaussian_spec_color = new System.Windows.Media.Color();
            gaussian_spec_color = System.Windows.Media.Color.FromRgb(255, 255, 255);

            ////Add this after 2/4/14
            ////DrawAsWireframe = false;
        }
    }

    public enum RenderMethod
    {
        [Description("Type")]
        CELL_TYPE,
        [Description("Population")]
        CELL_POP,
        [Description("Differentiation State (solid color)")]
        CELL_DIFF_STATE,
        [Description("Differentiation State (shade)")]
        CELL_DIFF_SHADE,
        [Description("Division State (solid color)")]
        CELL_DIV_STATE,
        [Description("Division State (shade)")]
        CELL_DIV_SHADE,
        [Description("Death State (solid color)")]
        CELL_DEATH_STATE,
        [Description("Death State (shade)")]
        CELL_DEATH_SHADE,
        [Description("Generation (solid color)")]
        CELL_GEN,
        [Description("Generation (shade)")]
        CELL_GEN_SHADE,
        [Description("Molecular Population (continuous)")]
        MP_CONC,
        [Description("Molecular Population (discrete)")]
        MP_CONC_SHADE,
        [Description("MolecularPopulation (Mixed Colors)")]
        MP_CONC_MIX_COLOR
    }

    public class RenderColor : INotifyPropertyChanged
    {
        //for future use, this is a hint as to what is this color being used, such as for the diff state of
        public string hint { get; set; } //this is a hint as to what is this color being used, such as for the diff state of 

        private Color _entityColor;

        public Color EntityColor
        {
            get { return _entityColor; }
            set
            {
                if (_entityColor != value)
                {
                    _entityColor = value;
                    OnPropertyChanged("EntityColor");
                }
            }

        }

        public RenderColor() { }

        public RenderColor(Color c)
        {
            hint = "";
            EntityColor = c;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

    }

    public class RenderCell : EntityModelBase
    {
        public RenderColor base_color { get; set; }         //solid color for applicable render methods

        public ObservableCollection<RenderColor> cell_pop_colors { get; set; }
        public ObservableCollection<RenderColor> diff_state_colors { get; set; }
        public ObservableCollection<RenderColor> diff_shade_colors { get; set; }
        public ObservableCollection<RenderColor> div_state_colors { get; set; }
        public ObservableCollection<RenderColor> div_shade_colors { get; set; }
        public ObservableCollection<RenderColor> death_state_colors { get; set; }
        public ObservableCollection<RenderColor> death_shade_colors { get; set; }
        public ObservableCollection<RenderColor> gen_colors { get; set; }
        public ObservableCollection<RenderColor> gen_shade_colors { get; set; }

        public int shades { get; set; }                                  // number of shades for applicable options

        private string _name;
        public string name
        {

            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("name");
                }
            }
        }

        public string renderLabel { get; set; }                                // ConfigCell's label

        public RenderCell()
        {
            cell_pop_colors = new ObservableCollection<RenderColor>();

            diff_state_colors = new ObservableCollection<RenderColor>();
            diff_shade_colors = new ObservableCollection<RenderColor>();

            div_state_colors = new ObservableCollection<RenderColor>();
            div_shade_colors = new ObservableCollection<RenderColor>();

            death_state_colors = new ObservableCollection<RenderColor>();
            death_shade_colors = new ObservableCollection<RenderColor>();

            gen_colors = new ObservableCollection<RenderColor>();
            gen_shade_colors = new ObservableCollection<RenderColor>();

            renderLabel = "";
        }
    }

    public class RenderMol
    {

        public RenderColor color { get; set; }
        public double min { get; set; }             // to scale when rendering by conc
        public double max { get; set; }
        public int shades { get; set; }             // number of shades for applicable options
        public double blendingWeight { get; set; }  // controls color mixing for multiple molpops
        public string name { get; set; }            // exist to facilitate eding scheme
        public string renderLabel { get; set; }     // ConfigMolecule’s label

        [JsonIgnore]
        private List<Color> shade_colors { get; set; }

        public RenderMol()
        {
            renderLabel = "";
        }

        /// <summary>
        /// this is for testing the method of using different color for different conc. for a molpop
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Color GetConcColor(int index)
        {
            if (shade_colors == null)
            {
                shade_colors = ColorHelper.pickASetOfColor(12);
            }
            return shade_colors[index % 12];
        }

    }

    public class RenderPop
    {
        public bool renderOn { get; set; }                 // toggles rendering
        public RenderMethod renderMethod { get; set; }      // indicates the render option
        public string name { get; set; }                    // exist to facilitate eding scheme
        public string renderLabel { get; set; }                   // cell or mol population's label

        public RenderPop()
        {
            renderLabel = "";
        }

    }

    public class Render3DView
    {
        public Color bg_color { get; set; }     // the background color

        public Render3DView()
        {
            bg_color = Color.FromScRgb(255.0f, 255.0f, 255.0f, 255.0f);
        }
    }

    public class RenderSkin
    {

        public string Name { get; set; }

        [JsonIgnore]
        public string originalContent { get; set; }

        [JsonIgnore]
        public string FileName { get; set; }

        public ObservableCollection<RenderCell> renderCells { get; set; }
        public ObservableCollection<RenderMol> renderMols { get; set; }

        public RenderSkin()
        {
            renderCells = new ObservableCollection<RenderCell>();
            renderMols = new ObservableCollection<RenderMol>();
        }

        /// <summary>
        /// create a new skin add add components corresponding to the content of entity repository
        /// </summary>
        public RenderSkin(string name, EntityRepository er)
        {
            this.Name = name;
            renderCells = new ObservableCollection<RenderCell>();
            renderMols = new ObservableCollection<RenderMol>();
            if (er == null) return;

            ColorHelper.resetColorPicker();
            for (int i = 0; i < er.cells.Count; i++)
            {
                var cell = er.cells[i];
                ColorHelper.resetColorPicker(i);
                AddRenderCell(cell.entity_guid, cell.CellName);
            }

            ColorHelper.resetColorPicker();
            for (int i = 0; i < er.molecules.Count; i++)
            {
                var mol = er.molecules[i];
                AddRenderMol(mol.entity_guid, mol.Name);
            }

        }

        //default color
        public void AddRenderCell(string label, string name)
        {            
            //Don't want duplicates but if name has changed, update it
            if (renderCells.Any(c => c.renderLabel == label) == true)
            {
                RenderCell cell = renderCells.First(c => c.renderLabel == label);
                if (cell.name != name)
                {
                    cell.name = name;
                }
                return;
            }

            RenderCell renc = new RenderCell();
            renc.renderLabel = label;
            renc.name = name;

            //try to resetColorPicker to pick a different color from last one.
            for (int i = renderCells.Count - 1; i >= 0; i--)
            {
                Color c = renderCells[i].base_color.EntityColor;
                if (ColorHelper.resetColorPicker(c) == true) break;
            }

            renc.base_color = new RenderColor(ColorHelper.pickASolidColor());


            //cell_pop
            ColorHelper.resetColorPicker();
            List<Color> colorlist = ColorHelper.pickASetOfColor(8);
            foreach (Color color in colorlist)
            {
                renc.cell_pop_colors.Add(new RenderColor(color));
            }

            //diff_state
            colorlist = ColorHelper.pickASetOfColor(8);
            foreach (Color color in colorlist)
            {
                renc.diff_state_colors.Add(new RenderColor(color));
            }

            //diff_state_shade
            colorlist = ColorHelper.pickColorShades(colorlist[0], 8, true);
            foreach (Color color in colorlist)
            {
                renc.diff_shade_colors.Add(new RenderColor(color));
            }

            //div_state
            colorlist = ColorHelper.pickASetOfColor(8);
            foreach (Color color in colorlist)
            {
                renc.div_state_colors.Add(new RenderColor(color));
            }

            //div_state_shade
            //colorlist = ColorHelper.pickColorShades(ColorHelper.pickASolidColor(), 8);
            colorlist = ColorHelper.pickColorShades(colorlist[0], 8, true);
            foreach (Color color in colorlist)
            {
                renc.div_shade_colors.Add(new RenderColor(color));
            }

            //death_state
            colorlist = ColorHelper.pickASetOfColor(2);
            foreach (Color color in colorlist)
            {
                renc.death_state_colors.Add(new RenderColor(color));
            }

            //death_state_shade
            //colorlist = ColorHelper.pickColorShades(ColorHelper.pickASolidColor(), 2);
            colorlist = ColorHelper.pickColorShades(colorlist[0], 2, true);
            foreach (Color color in colorlist)
            {
                renc.death_shade_colors.Add(new RenderColor(color));
            }

            //gen_colors
            colorlist = ColorHelper.pickASetOfColor(12);
            foreach (Color color in colorlist)
            {
                renc.gen_colors.Add(new RenderColor(color));
            }

            //gene_color_shade
            //colorlist = ColorHelper.pickColorShades(ColorHelper.pickASolidColor(), 12);
            colorlist = ColorHelper.pickColorShades(colorlist[0], 12, true);
            foreach (Color color in colorlist)
            {
                renc.gen_shade_colors.Add(new RenderColor(color));
            }
            
            renderCells.Add(renc);
        }

        /// <summary>
        /// Method for updating render cell name
        /// </summary>
        /// <param name="label"></param>
        /// <param name="name"></param>
        public void SetRenderCellName(string label, string name)
        {
            if (renderCells.Any(c => c.renderLabel == label) == true)
            {
                RenderCell cell = renderCells.First(c => c.renderLabel == label);
                if (cell.name != name)
                {
                    cell.name = name;
                }
            }
        }

        public void AddRenderMol(string label, string name)
        {
            //Don't want duplicates but if name has changed, update it
            if (renderMols.Any(m => m.renderLabel == label) == true)
            {
                RenderMol rm = renderMols.First(m => m.renderLabel == label);
                rm.name = name;
                return;
            }

            RenderMol renm = new RenderMol();
            renm.renderLabel = label;
            renm.name = name;
            renm.min = 0.0;
            renm.max = 1.0;
            renm.blendingWeight = 10.0;

            //try to resetColorPicker to pick a different color from last one.
            for (int i = renderMols.Count - 1; i >= 0; i--)
            {
                Color c = renderMols[i].color.EntityColor;
                if (ColorHelper.resetColorPicker(c) == true) break;
            }
            renm.color = new RenderColor(ColorHelper.pickASolidColor());
            renm.shades = 10;
            renderMols.Add(renm);
        }

        public void SetRenderMolName(string label, string name)
        {
            if (renderMols.Any(c => c.renderLabel == label) == true)
            {
                RenderMol rm = renderMols.First(c => c.renderLabel == label);
                if (rm.name != name)
                {
                    rm.name = name;
                }
            }
        }


        //Serialization method needed
        public static RenderSkin DeserializeFromFile(string jsonFile)
        {
            string readText = File.ReadAllText(jsonFile);
            RenderSkin skin = JsonConvert.DeserializeObject<RenderSkin>(readText);
            skin.FileName = jsonFile;
            skin.originalContent = readText;
            return skin;
        }

        public void SerializeToFile(string filename = null)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;

            //serialize RenderSkin
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = filename == null ? FileName : filename;

            try
            {
                File.WriteAllText(jsonFile, jsonSpec);
            }
            catch
            {
                MessageBox.Show("File.WriteAllText failed in SerializeToFile. Filename and TempFile = " + FileName + ", " + filename);
            }
        }

        /// <summary>
        /// check if content changed
        /// </summary>
        public void SaveChanges()
        {
            //check if changed.
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;

            //serialize RenderSkin
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);

            if (jsonSpec == originalContent) return;
            SerializeToFile();
            this.originalContent = jsonSpec;
        }
    }

    public class RenderPopOptions
    {
        public ObservableCollection<RenderPop> cellPopOptions { get; set; }
        public ObservableCollection<RenderPop> molPopOptions { get; set; }

        public RenderPopOptions()
        {
            cellPopOptions = new ObservableCollection<RenderPop>();
            molPopOptions = new ObservableCollection<RenderPop>();
        }

        /// <summary>
        /// add render options for a population
        /// </summary>
        /// <param name="lable"></param>
        /// <param name="isCell"></param>
        public void AddRenderOptions(string lable, string name, bool isCell)
        {
            if (isCell)
            {
                //add if not exist
                bool entry_exist = cellPopOptions.Any(item => item.renderLabel == lable);
                if (entry_exist) return;
                RenderPop rp = new RenderPop();
                rp.renderLabel = lable;
                rp.name = name;
                rp.renderOn = true;
                rp.renderMethod = RenderMethod.CELL_TYPE;
                cellPopOptions.Add(rp);
            }
            else
            {
                bool entry_exist = molPopOptions.Any(item => item.renderLabel == lable);
                if (entry_exist) return;
                RenderPop rp = new RenderPop();
                rp.renderLabel = lable;
                rp.name = name;
                rp.renderOn = false;
                rp.renderMethod = RenderMethod.MP_CONC;
                molPopOptions.Add(rp);
            }
        }

        public void RemoveRenderOptions(string lable, bool isCell)
        {
            if (isCell)
            {
                RenderPop item = cellPopOptions.Where(x => x.renderLabel == lable).FirstOrDefault();
                if (item != null)
                {
                    cellPopOptions.Remove(item);
                }
            }
            else
            {
                RenderPop item = molPopOptions.Where(x => x.renderLabel == lable).FirstOrDefault();
                if (item != null)
                {
                    molPopOptions.Remove(item);
                }
            }
        }

        public RenderPop GetCellRenderPop(string label)
        {
            return this.cellPopOptions.Where(x => x.renderLabel == label).SingleOrDefault();
        }

        public RenderPop GetMolRenderPop(string label)
        {
            return this.molPopOptions.Where(x => x.renderLabel == label).SingleOrDefault();
        }
    }


    // UTILITY CLASSES =======================
    public class BoxSpecification : EntityModelBase
    {
        public string box_guid { get; set; }
        public double[][] transform_matrix { get; set; }
        private bool _box_visibility = true;
        private bool _current_box_visibility = true;

        // Range values calculated based on environment extents
#if USE_BOX_LIMITS
        private double _x_trans_max;
        private double _x_trans_min;
        private double _x_scale_max;
        private double _x_scale_min;
        private double _y_trans_max;
        private double _y_trans_min;
        private double _y_scale_max;
        private double _y_scale_min;
        private double _z_trans_max;
        private double _z_trans_min;
        private double _z_scale_max;
        private double _z_scale_min;

        [XmlIgnore]
        public double x_trans_max
        {
            get { return _x_trans_max; }
            set
            {
                if (_x_trans_max == value)
                    return;
                else
                {
                    _x_trans_max = value;
                    OnPropertyChanged("x_trans_max");
                    // This doesn't seem to be taken care of by GUI itself...
                    if (x_trans > _x_trans_max) x_trans = _x_trans_max;
                }
            }
        }

        [XmlIgnore]
        public double x_trans_min
        {
            get { return _x_trans_min; }
            set
            {
                if (_x_trans_min == value)
                    return;
                else
                {
                    _x_trans_min = value;
                    OnPropertyChanged("x_trans_min");
                    // This doesn't seem to be taken care of by GUI itself...
                    if (x_trans < _x_trans_min) x_trans = _x_trans_min;
                }
            }
        }

        [XmlIgnore]
        public double x_scale_max
        {
            get { return _x_scale_max; }
            set
            {
                if (_x_scale_max == value)
                    return;
                else
                {
                    _x_scale_max = value;
                    OnPropertyChanged("x_scale_max");
                    // This doesn't seem to be taken care of by GUI itself...
                    if (x_scale > _x_scale_max) x_scale = _x_scale_max;
                }
            }
        }

        [XmlIgnore]
        public double x_scale_min
        {
            get { return _x_scale_min; }
            set
            {
                if (_x_scale_min == value)
                    return;
                else
                {
                    _x_scale_min = value;
                    OnPropertyChanged("x_scale_min");
                    // This doesn't seem to be taken care of by GUI itself...
                    if (x_scale < _x_scale_min) x_scale = _x_scale_min;
                }
            }
        }

        [XmlIgnore]
        public double y_trans_max
        {
            get { return _y_trans_max; }
            set
            {
                if (_y_trans_max == value)
                    return;
                else
                {
                    _y_trans_max = value;
                    OnPropertyChanged("y_trans_max");
                    if (y_trans > _y_trans_max) y_trans = _y_trans_max;
                }
            }
        }

        [XmlIgnore]
        public double y_trans_min
        {
            get { return _y_trans_min; }
            set
            {
                if (_y_trans_min == value)
                    return;
                else
                {
                    _y_trans_min = value;
                    OnPropertyChanged("y_trans_min");
                    if (y_trans < _y_trans_min) y_trans = _y_trans_min;
                }
            }
        }

        [XmlIgnore]
        public double y_scale_max
        {
            get { return _y_scale_max; }
            set
            {
                if (_y_scale_max == value)
                    return;
                else
                {
                    _y_scale_max = value;
                    OnPropertyChanged("y_scale_max");
                    if (y_scale > _y_scale_max) y_scale = _y_scale_max;
                }
            }
        }

        [XmlIgnore]
        public double y_scale_min
        {
            get { return _y_scale_min; }
            set
            {
                if (_y_scale_min == value)
                    return;
                else
                {
                    _y_scale_min = value;
                    OnPropertyChanged("y_scale_min");
                    if (y_scale < _y_scale_min) y_scale = _y_scale_min;
                }
            }
        }

        [XmlIgnore]
        public double z_trans_max
        {
            get { return _z_trans_max; }
            set
            {
                if (_z_trans_max == value)
                    return;
                else
                {
                    _z_trans_max = value;
                    OnPropertyChanged("z_trans_max");
                    if (z_trans > _z_trans_max) z_trans = _z_trans_max;
                }
            }
        }

        [XmlIgnore]
        public double z_trans_min
        {
            get { return _z_trans_min; }
            set
            {
                if (_z_trans_min == value)
                    return;
                else
                {
                    _z_trans_min = value;
                    OnPropertyChanged("z_trans_min");
                    if (z_trans < _z_trans_min) z_trans = _z_trans_min;
                }
            }
        }

        [XmlIgnore]
        public double z_scale_max
        {
            get { return _z_scale_max; }
            set
            {
                if (_z_scale_max == value)
                    return;
                else
                {
                    _z_scale_max = value;
                    OnPropertyChanged("z_scale_max");
                    if (z_scale > _z_scale_max) z_scale = _z_scale_max;
                }
            }
        }

        [XmlIgnore]
        public double z_scale_min
        {
            get { return _z_scale_min; }
            set
            {
                if (_z_scale_min == value)
                    return;
                else
                {
                    _z_scale_min = value;
                    OnPropertyChanged("z_scale_min");
                    if (z_scale < _z_scale_min) z_scale = _z_scale_min;
                }
            }
        }
#endif

        public bool box_visibility
        {
            get { return _box_visibility; }
            set
            {
                if (_box_visibility == value)
                    return;
                else
                {
                    _box_visibility = value;
                    OnPropertyChanged("box_visibility");
                }
            }
        }
        public bool current_box_visibility
        {
            get { return _current_box_visibility; }
            set
            {
                if (_current_box_visibility == value)
                    return;
                else
                {
                    _current_box_visibility = value;
                    OnPropertyChanged("current_box_visibility");
                }
            }
        }
        public double x_scale
        {
            get
            {
                return getScale(0);
            }
            set
            {
                double current = getScale(0);

                if (value != current)
                {
                    if (current != 0.0)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            transform_matrix[i][0] /= current;
                            transform_matrix[i][0] *= value;
                        }
                    }
                    else
                    {
                        transform_matrix[0][0] = value;
                        transform_matrix[1][0] = 0.0;
                        transform_matrix[2][0] = 0.0;
                    }
                    base.OnPropertyChanged("x_scale");
                }
            }
        }
        public double y_scale
        {
            get
            {
                return getScale(1);
            }
            set
            {
                double current = getScale(1);

                if (value != current)
                {
                    if (current != 0.0)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            transform_matrix[i][1] /= current;
                            transform_matrix[i][1] *= value;
                        }
                    }
                    else
                    {
                        transform_matrix[0][1] = 0.0;
                        transform_matrix[1][1] = value;
                        transform_matrix[2][1] = 0.0;
                    }
                    base.OnPropertyChanged("y_scale");
                }
            }
        }
        public double z_scale
        {
            get
            {
                return getScale(2);
            }
            set
            {
                double current = getScale(2);

                if (value != current)
                {
                    if (current != 0.0)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            transform_matrix[i][2] /= current;
                            transform_matrix[i][2] *= value;
                        }
                    }
                    else
                    {
                        transform_matrix[0][2] = 0.0;
                        transform_matrix[1][2] = 0.0;
                        transform_matrix[2][2] = value;
                    }
                    base.OnPropertyChanged("z_scale");
                }
            }
        }

        public double x_trans
        {
            get { return transform_matrix[0][3]; }
            set
            {
                if (value != transform_matrix[0][3])
                {
                    transform_matrix[0][3] = value;
                    base.OnPropertyChanged("x_trans");
                }
            }
        }
        public double y_trans
        {
            get { return transform_matrix[1][3]; }
            set
            {
                if (value != transform_matrix[1][3])
                {
                    transform_matrix[1][3] = value;
                    base.OnPropertyChanged("y_trans");
                }
            }
        }
        public double z_trans
        {
            get { return transform_matrix[2][3]; }
            set
            {
                if (value != transform_matrix[2][3])
                {
                    transform_matrix[2][3] = value;
                    base.OnPropertyChanged("z_trans");
                }
            }
        }

        public double getScale(byte i)
        {
            if (i >= 3)
            {
                return 0.0;
            }

            double scale = Math.Sqrt(transform_matrix[0][i] * transform_matrix[0][i] +
                                     transform_matrix[1][i] * transform_matrix[1][i] +
                                     transform_matrix[2][i] * transform_matrix[2][i]);
            return scale;
        }

        [JsonIgnore]
        public double half_x_scale
        {
            get
            {
                return x_scale / 2;
            }
            set
            {
                x_scale = 2 * value;
                base.OnPropertyChanged("x_scale");
            }
        }
        [JsonIgnore]
        public double half_y_scale
        {
            get
            {
                return y_scale / 2;
            }
            set
            {
                y_scale = 2 * value;
                base.OnPropertyChanged("y_scale");
            }
        }
        [JsonIgnore]
        public double half_z_scale
        {
            get
            {
                return z_scale / 2;
            }
            set
            {
                z_scale = 2 * value;
                base.OnPropertyChanged("z_scale");
            }
        }

        public BoxSpecification()
        {
            Guid id = Guid.NewGuid();

            box_guid = id.ToString();
            box_visibility = true;
            current_box_visibility = true;
            transform_matrix = new double[][] { new double[]{1.0, 0.0, 0.0, 0.0},
                                                new double[]{0.0, 1.0, 0.0, 0.0},
                                                new double[]{0.0, 0.0, 1.0, 0.0},
                                                new double[]{0.0, 0.0, 0.0, 1.0} };
        }

#if USE_BOX_LIMITS
        public void SetBoxSpecExtents(ConfigECSEnvironment environment)
        {
            x_scale_max = environment.extent_x;
            x_scale_min = environment.extent_min;
            x_trans_max = 1.5 * environment.extent_x;
            x_trans_min = -environment.extent_x / 2.0;

            y_scale_max = environment.extent_y;
            y_scale_min = environment.extent_min;
            y_trans_max = 1.5 * environment.extent_y;
            y_trans_min = -environment.extent_y / 2.0;

            z_scale_max = environment.extent_z;
            z_scale_min = environment.extent_min;
            z_trans_max = 1.5 * environment.extent_z;
            z_trans_min = -environment.extent_z / 2.0;
        }
#endif

        public void SetMatrix(double[][] value)
        {
            bool x_scale_change = false,
                 y_scale_change = false,
                 z_scale_change = false,
                 matrix_change = false;

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (value[row][col] != transform_matrix[row][col])
                    {
                        transform_matrix[row][col] = value[row][col];
                        // call handler once only
                        if (matrix_change == false)
                        {
                            matrix_change = true;
                        }

                        // handle scaling
                        if (x_scale_change == false && row < 3 && col == 0)
                        {
                            x_scale_change = true;
                        }
                        else if (y_scale_change == false && row < 3 && col == 1)
                        {
                            y_scale_change = true;
                        }
                        else if (z_scale_change == false && row < 3 && col == 2)
                        {
                            z_scale_change = true;
                        }

                        // handle translations
                        else if (row == 0 && col == 3)
                        {
                            base.OnPropertyChanged("x_trans");
                        }
                        else if (row == 1 && col == 3)
                        {
                            base.OnPropertyChanged("y_trans");
                        }
                        else if (row == 2 && col == 3)
                        {
                            base.OnPropertyChanged("z_trans");
                        }
                    }
                }
            }

            // call property changed handlers
            if (matrix_change == true)
            {
                base.OnPropertyChanged("transform_matrix");
            }
            if (x_scale_change == true)
            {
                base.OnPropertyChanged("x_scale");
                base.OnPropertyChanged("half_x_scale");
            }
            if (y_scale_change == true)
            {
                base.OnPropertyChanged("y_scale");
                base.OnPropertyChanged("half_y_scale");
            }
            if (z_scale_change == true)
            {
                base.OnPropertyChanged("z_scale");
                base.OnPropertyChanged("half_z_scale");
            }
        }

        public void BoxChangedEventHandler(object obj, EventArgs e)
        {
            // not sure if anything goes here.
        }
    }

    /// <summary>
    /// Converter to test > 1 -> true, not -> false for "s" plural addition
    /// </summary>
    [ValueConversion(typeof(int), typeof(bool))]
    public class ManyToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return ((int)value) > 1;
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }

    /// <summary>
    /// Convert Reporter enum to boolean
    /// </summary>
    [ValueConversion(typeof(ExtendedReport), typeof(bool))]
    public class RptEnumBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            bool ret = parameterValue.Equals(value);
            return ret;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            bool chk = (bool)value;

            if (chk == false)
                return Enum.Parse(targetType, "NONE");

            return Enum.Parse(targetType, parameterString);
        }
        #endregion
    }


    /*
    [ValueConversion(typeof(RenderMethod), typeof(bool))]
    public class CellRenderMethodConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            bool ret = parameterValue.Equals(value);
            return ret;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            bool chk = (bool)value;

            if (chk == false)
            {
                //types start with MP_ is for molpops in ECS
                if (parameterString.StartsWith("MP_"))
                {
                    return RenderMethod.MP_TYPE;
                }
                else
                {
                    return RenderMethod.CELL_TYPE;
                }
            }
            return Enum.Parse(targetType, parameterString);
        }
        #endregion
    }

    */

    public class RenderMethodItemValidConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return true;
            var strtype = value.ToString();

            var para = parameter as string;
            if (para == "cell")
            {
                return strtype.StartsWith("CELL_");
            }
            else
            {
                return !strtype.StartsWith("CELL_");
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }





    /// <summary>
    /// Base class for all EntityModel classes.
    /// It provides support for property change notifications 
    /// and disposal.  This class is abstract.
    /// </summary>
    public abstract class EntityModelBase : INotifyPropertyChanged, IDisposable
    {
        #region Constructor

        protected EntityModelBase()
        {
        }

        #endregion // Constructor

        #region DisplayName

        /// <summary>
        /// Returns the user-friendly name of this object.
        /// Child classes can set this property to a new value,
        /// or override it to determine the value on-demand.
        /// </summary>
        // public virtual string DisplayName { get; protected set; }

        #endregion // DisplayName

        #region Debugging Aides

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                if (this.ThrowOnInvalidPropertyName)
                    throw new Exception(msg);
                else
                    Debug.Fail(msg);
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        #endregion // Debugging Aides

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.VerifyPropertyName(propertyName);

            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);

            }
        }

        #endregion // INotifyPropertyChanged Members

        #region IDisposable Members

        /// <summary>
        /// Invoked when this object is being removed from the application
        /// and will be subject to garbage collection.
        /// </summary>
        public void Dispose()
        {
            this.OnDispose();
        }

        /// <summary>
        /// Child classes can override this method to perform 
        /// clean-up logic, such as removing event handlers.
        /// </summary>
        protected virtual void OnDispose()
        {
        }

#if DEBUG
        /// <summary>
        /// Useful for ensuring that ViewModel objects are properly garbage collected.
        /// </summary>
        ~EntityModelBase()
        {
            // string msg = string.Format("{0} ({1}) ({2}) Finalized", this.GetType().Name, this.DisplayName, this.GetHashCode());
            // string msg = string.Format("{0} ({1}) Finalized", this.GetType().Name, this.GetHashCode());
            // Console.WriteLine(String.Format(msg));
        }
#endif

        #endregion // IDisposable Members
    }

    /// <summary>
    /// Class to handle parameters whose values may be set by a probability distribution.
    /// The default is a constant value.
    /// If there is a distribution on the parameter, then ConstValue doesn't have any relevance to the value of the parameter.
    /// In either case, the parameter value should be obtained using the Sample method.
    /// </summary>
    public class DistributedParameter : EntityModelBase
    {
        private ParameterDistribution paramDistr;
        public ParameterDistribution ParamDistr 
        {
            get
            {
                return paramDistr;
            }
            set
            {
                paramDistr = value;
                OnPropertyChanged("ParamDistr");
            }
        }

        // Value of parameter when constant - no distribution.
        private double constValue;
        public double ConstValue 
        {
            get
            {
                return constValue;
            }
            set
            {
                constValue = value;
                OnPropertyChanged("ConstValue");
            }
        }

        private ParameterDistributionType distributionType;
        public ParameterDistributionType DistributionType
        {
            get
            {
                return distributionType;
            }
            set
            {
                distributionType = value;
                OnPropertyChanged("DistributionType");
            }
        }

        public DistributedParameter()
        {
            DistributionType = ParameterDistributionType.CONSTANT;
        }

        /// <summary>
        /// Caution if calling this constructor from another constructor.
        /// May cause problems when deserializing json.
        /// </summary>
        /// <param name="_constValue"></param>
        public DistributedParameter(double _constValue)
        {
            ConstValue = _constValue;
            DistributionType = ParameterDistributionType.CONSTANT;
        }    

        public double Sample()
        {
            if (ParamDistr == null)
            {
                return ConstValue;
            }
            else
            {
                return ParamDistr.Sample();
            }
        }

        public void Reset()
        {
            if (ParamDistr != null)
            {
                ParamDistr.isInitialized = false;
            }
        }
    }

    /// <summary>
    /// Types of probability distributions for distributed parameters.
    /// </summary>
    public enum ParameterDistributionType { CONSTANT=0, POISSON, GAMMA, UNIFORM, CATEGORICAL };

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(ParameterDistributionType), typeof(string))]
    public class ParameterDistributionTypeToStringConverter : IValueConverter
    {
        private List<string> _param_dist_type_strings = new List<string>()
                                {
                                    "Constant",
                                    "Poisson",
                                    "Gamma",
                                    "Uniform",
                                    "Categorical"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value as string == "") return "Constant";
            try
            {
                return _param_dist_type_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _param_dist_type_strings.FindIndex(item => item == str);
            return (ParameterDistributionType)Enum.ToObject(typeof(ParameterDistributionType), (int)idx);
        }
    }

    /// <summary>
    /// Convert:
    ///     Converter to go between enum values and boolean for GUI
    ///     If the parameter distribution type is CONSTANT, then return False.
    ///     Return True for all other distribution types.
    ///  ConvertBack: 
    ///     Shouldn't be used. Return CONSTANT.
    /// </summary>
    [ValueConversion(typeof(ParameterDistributionType), typeof(string))]
    public class ParameterDistributionTypeToBoolConverter : IValueConverter
    {
        
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ParameterDistributionType pdt;
            try
            {
                pdt = (ParameterDistributionType)value;
            }
            catch
            {
                pdt = ParameterDistributionType.CONSTANT;
            }

            if (pdt == ParameterDistributionType.CONSTANT)
            {
                return false;

            }
            else
            {
                return true;
            }
        }


        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Shouldn't be using this, so simply default to constant 
            return ParameterDistributionType.CONSTANT;
        }
    }

    /// <summary>
    /// Abstract class for probability distributions on parameters.
    /// </summary>
    public abstract class ParameterDistribution : EntityModelBase
    {
        public ParameterDistributionType DistributionType { get; set; }
        [JsonIgnore]
        public bool isInitialized;

        public ParameterDistribution(ParameterDistributionType _distributionType)
        {
            DistributionType = _distributionType;
            isInitialized = false;
        }
        public abstract void Initialize();
        public abstract double Sample();
    }

    /// <summary>
    /// Probability distribution for the parameter is uniform.
    /// </summary>
    public class UniformParameterDistribution : ParameterDistribution
    {
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        [JsonIgnore]
        private ContinuousUniform UniformDist;

        public UniformParameterDistribution()
            : base(ParameterDistributionType.UNIFORM)
        {
        }

        public override void Initialize()
        {
            if (MaxValue <= MinValue)
            {
                MessageBox.Show("The max value must be greater than the min value in a Uniform distribution. The range has been set to (0,1)");
                MinValue = 0.0;
                MaxValue = 1.0;
            }

            UniformDist = new ContinuousUniform(MinValue, MaxValue, Rand.MersenneTwister);
            isInitialized = true;
        }

        public override double Sample()
        {
            if (isInitialized == false)
            {
                Initialize();
            }

            return UniformDist.Sample();
        }
    }

    /// <summary>
    /// Probability distribution for the parameter is a Poisson distribution.
    /// </summary>
    public class PoissonParameterDistribution : ParameterDistribution
    {
        public double Mean { get; set; }
        [JsonIgnore]
        private Poisson PoissonDist;

        public PoissonParameterDistribution()
            : base(ParameterDistributionType.POISSON)
        {
        }

        public override void Initialize()
        {
            if (Mean <= 0)
            {
                MessageBox.Show("Lambda must be greater than zero in a Poisson distribution. Lambda has been set to 1.0.");
                Mean = 1.0;
            }

            PoissonDist = new Poisson(Mean, Rand.MersenneTwister);
            isInitialized = true;
        }

        public override double Sample()
        {
            if (isInitialized == false)
            {
                Initialize();
            }

            return (double)PoissonDist.Sample();
        }
    }

    /// <summary>
    /// Probability distribution for the parameter is a Gamma distribution.
    /// </summary>
    public class GammaParameterDistribution : ParameterDistribution
    {
        public double Shape { get; set; }
        public double Rate { get; set; }
        [JsonIgnore]
        private Gamma GammaDist;

        public GammaParameterDistribution()
            : base(ParameterDistributionType.GAMMA)
        {
        }

        public override void Initialize()
        {
            if (Rate <= 0)
            {
                MessageBox.Show("The rate parameter must be greater than zero in a Gamma distribution. The rate parameter has been set to 1.0.");
                Rate = 1.0;
            }
            if (Shape <= 0)
            {
                MessageBox.Show("The shape parameter must be greater than zero in a Poisson distribution. The shape parameter has been set to 1.0.");
            }

            GammaDist = new Gamma(Shape, Rate, Rand.MersenneTwister);
            isInitialized = true;
        }

        public override double Sample()
        {
            if (isInitialized == false)
            {
                Initialize();
            }

            return GammaDist.Sample();
        }
    }

    /// <summary>
    /// Class for categorical item for categorical distribution.
    /// Contains the parameter value (CategoryValue) and it's probability (Prob)
    /// </summary>
    public class CategoricalDistrItem : EntityModelBase
    {
        private double categoryValue;
        public double CategoryValue
        {
            get
            {
                return categoryValue;
            }

            set
            {
                categoryValue = value;
                OnPropertyChanged("CategoryValue");
            }
        }

        private double prob;
        public double Prob
        {
            get
            {
                return prob;
            }

            set
            {
                prob = value;
                OnPropertyChanged("Prob");
            }
        }

        public CategoricalDistrItem(double _value, double _prob)
        {
            CategoryValue = _value;
            Prob = _prob;
        }
    }

    /// <summary>
    /// Probability distribution for the parameter is a categorical distribution.
    /// </summary>
    public class CategoricalParameterDistribution : ParameterDistribution
    {
        private ObservableCollection<CategoricalDistrItem> probMass;
        public ObservableCollection<CategoricalDistrItem> ProbMass
        {
            get
            {
                return probMass;
            }

            set
            {
                probMass = value;
                OnPropertyChanged("ProbMass");
            }
        }

        [JsonIgnore]
        public Categorical CategoricalDist;

        public CategoricalParameterDistribution()
            : base(ParameterDistributionType.CATEGORICAL)
        {
            probMass = new ObservableCollection<CategoricalDistrItem>();
        }

        public double[] ProbArray()
        {
            double[] probArray = new double[probMass.Count()];
            int cnt = 0;

            foreach (CategoricalDistrItem cdi in probMass)
            {
                probArray[cnt++] = cdi.Prob;
            }

            return probArray;
        }

        public override void Initialize()
        {
            if (ProbMass.Count == 0)
            {
                MessageBox.Show("Warning. No categories in the distribution. A distribution with binary categories of equal probability has been created. ");
                probMass.Add(new CategoricalDistrItem(0.0, 0.5));
                probMass.Add(new CategoricalDistrItem(1.0, 0.5));
            }
            Normalize();
            CategoricalDist = new Categorical(ProbArray(), Rand.MersenneTwister);
            isInitialized = true;
        }

        public void Normalize()
        {
            double sum = 0;
            sum = ProbArray().Sum();
            
            for (int i = 0; i < ProbMass.Count; i++)
            {
                ProbMass[i].Prob /= sum;
            }

            OnPropertyChanged("ProbMass");
        }

        public override double Sample()
        {
            if (isInitialized == false)
            {
                Initialize();
            }

            int i = (int)CategoricalDist.Sample();

            return probMass[i].CategoryValue;
        }
    }


}
