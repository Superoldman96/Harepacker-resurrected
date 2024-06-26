﻿/* Copyright (C) 2020 lastbattle

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

//uncomment the line below to create a space-time tradeoff (saving RAM by wasting more CPU cycles)
#define SPACETIME

using System;
using System.Windows.Forms;

namespace HaCreator.GUI.InstanceEditor
{
    public partial class LoadMapSelector : System.Windows.Forms.Form
    {
        /// <summary>
        /// The NumericUpDown text to set upon selection
        /// </summary>
        private NumericUpDown numericUpDown = null;

        /// <summary>
        /// Or the textbox
        /// </summary>
        private TextBox textBox = null;

        /// <summary>
        /// Load map selector
        /// </summary>
        /// <param name="numericUpDown"></param>
        public LoadMapSelector(NumericUpDown numericUpDown)
        {
            InitializeComponent();

            DialogResult = DialogResult.Cancel;

            this.numericUpDown = numericUpDown;
            this.searchBox.TextChanged += this.mapBrowser.searchBox_TextChanged;
        }

        /// <summary>
        /// Load map selector
        /// </summary>
        /// <param name="textbox"></param>
        public LoadMapSelector(TextBox textbox) {
            InitializeComponent();

            DialogResult = DialogResult.Cancel;

            this.textBox = textbox;
            this.searchBox.TextChanged += this.mapBrowser.searchBox_TextChanged;
        }

        /// <summary>
        /// On load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Load_Load(object sender, EventArgs e)
        {
            this.mapBrowser.InitializeMapsListboxItem(false); // load list of maps without Cash Shop, Login, etc
        }

        /// <summary>
        /// On load button clicked, selects that map and closes this dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadButton_Click(object sender, EventArgs e)
        {
            string mapid = mapBrowser.SelectedItem.Substring(0, 9);
            string mapcat = "Map" + mapid.Substring(0, 1);

            if (numericUpDown != null) {
                this.numericUpDown.Value = long.Parse(mapid);
            } else {
                this.textBox.Text = mapid;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void mapBrowser_SelectionChanged()
        {
        }

        private void Load_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                loadButton_Click(null, null);
            }
        }
    }
}
