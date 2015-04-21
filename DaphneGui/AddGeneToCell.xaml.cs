﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Daphne;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for AddGeneToCell.xaml
    /// </summary>
    public partial class AddGeneToCell : Window
    {
        public ConfigGene SelectedGene { get; set; }
        public ConfigCell SelectedCell { get; set; }
        public EntityRepository er { get; set; }

        public AddGeneToCell(ConfigCell cell)
        {
            InitializeComponent();
            this.Owner = Application.Current.MainWindow;

            //er = MainWindow.SOP.Protocol.entity_repository;
            Level level = MainWindow.GetLevelContext(this);
            er = level.entity_repository;

            //Protocol = sc;
            SelectedCell = cell;
            DataContext = this;

            GeneComboBox.Items.Clear();
            foreach (ConfigGene g in er.genes)
            {
                if (!SelectedCell.HasGene(g.entity_guid))
                {
                    GeneComboBox.Items.Add(g);
                }
            }
            //GeneComboBox.ItemsSource = er.genes;
            GeneComboBox.DisplayMemberPath = "Name";
            GeneComboBox.SelectedIndex = 0;

            if (GeneComboBox.Items.Count == 0)
            {
                MessageBox.Show("Please first add genes from the User store.", "No genes available", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SelectedGene = (ConfigGene)GeneComboBox.SelectedItem;
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
