﻿/*
 * Copyright 2019 faddenSoft
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Asm65;

namespace SourceGen.WpfGui {
    /// <summary>
    /// Symbol edit dialog.
    /// </summary>
    public partial class EditDefSymbol : Window, INotifyPropertyChanged {
        /// <summary>
        /// Result; will be set non-null on OK.
        /// </summary>
        public DefSymbol NewSym { get; private set; }

        /// <summary>
        /// Set to true when all fields are valid.  Controls whether the OK button is enabled.
        /// </summary>
        public bool IsValid {
            get { return mIsValid; }
            set { mIsValid = value; OnPropertyChanged(); }
        }
        private bool mIsValid;

        public string Label {
            get { return mLabel; }
            set { mLabel = value; OnPropertyChanged(); UpdateControls(); }
        }
        private string mLabel;

        public string Value {
            get { return mValue; }
            set { mValue = value; OnPropertyChanged(); UpdateControls(); }
        }
        private string mValue;

        public string VarWidth {
            get { return mWidth; }
            set { mWidth = value; OnPropertyChanged(); UpdateControls(); }
        }
        private string mWidth;

        public string Comment {
            get { return mComment; }
            set { mComment = value; OnPropertyChanged(); }
        }
        private string mComment;

        /// <summary>
        /// Format object to use when formatting addresses and constants.
        /// </summary>
        private Formatter mNumFormatter;

        /// <summary>
        /// Old symbol value.  May be null.
        /// </summary>
        private DefSymbol mOldSym;

        /// <summary>
        /// List of existing symbols, for uniqueness check.  The list will not be modified.
        /// </summary>
        private SortedList<string, DefSymbol> mDefSymbolList;

        /// <summary>
        /// Full symbol table, for extended uniqueness check.
        /// </summary>
        private SymbolTable mSymbolTable;

        // Saved off at dialog load time.
        private Brush mDefaultLabelColor;

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        /// <summary>
        /// Constructor, for editing a project symbol.
        /// </summary>
        public EditDefSymbol(Window owner, Formatter formatter,
                SortedList<string, DefSymbol> defList, DefSymbol defSym) {
            InitializeComponent();
            Owner = owner;
            DataContext = this;

            mNumFormatter = formatter;
            mDefSymbolList = defList;

            mOldSym = defSym;

            Label = Value = VarWidth = Comment = string.Empty;
            widthEntry1.Visibility = widthEntry2.Visibility = labelUniqueLabel.Visibility =
                Visibility.Collapsed;
            projectLabelUniqueLabel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Constructor, for editing a local variable.
        /// </summary>
        public EditDefSymbol(Window owner, Formatter formatter,
                SortedList<string, DefSymbol> defList, DefSymbol defSym,
                SymbolTable symbolTable) : this(owner, formatter, defList, defSym) {
            mSymbolTable = symbolTable;

            widthEntry1.Visibility = widthEntry2.Visibility = labelUniqueLabel.Visibility =
                Visibility.Visible;
            projectLabelUniqueLabel.Visibility = Visibility.Collapsed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mDefaultLabelColor = labelNotesLabel.Foreground;

            if (mOldSym != null) {
                Label = mOldSym.Label;
                Value = mNumFormatter.FormatValueInBase(mOldSym.Value,
                    mOldSym.DataDescriptor.NumBase);
                VarWidth = mOldSym.Width.ToString();
                Comment = mOldSym.Comment;

                if (mOldSym.SymbolType == Symbol.Type.Constant) {
                    constantRadioButton.IsChecked = true;
                } else {
                    addressRadioButton.IsChecked = true;
                }
            } else {
                addressRadioButton.IsChecked = true;
            }

            labelTextBox.Focus();
            UpdateControls();
        }

        private void UpdateControls() {
            if (!IsLoaded) {
                return;
            }

            // Label must be valid and not already exist in project symbol list.  (For project
            // symbols, it's okay if an identical label exists elsewhere.)
            bool labelValid = Asm65.Label.ValidateLabel(Label);
            bool labelUnique;

            if (mDefSymbolList.TryGetValue(Label, out DefSymbol existing)) {
                // It's okay if it's the same object.
                labelUnique = (existing == mOldSym);
            } else {
                labelUnique = true;
            }

            // For local variables, do a secondary uniqueness check.
            if (labelUnique && mSymbolTable != null) {
                labelUnique = !mSymbolTable.TryGetValue(Label, out Symbol sym);
            }

            // Value must be blank, meaning "erase any earlier definition", or valid value.
            // (Hmm... don't currently have a way to specify "no symbol" in DefSymbol.)
            //if (!string.IsNullOrEmpty(valueTextBox.Text)) {
            bool valueValid = ParseValue(out int unused1, out int unused2);
            //} else {
            //    valueValid = true;
            //}

            bool widthValid = true;
            if (widthEntry1.Visibility == Visibility.Visible) {
                if (!int.TryParse(VarWidth, out int width) ||
                        width < DefSymbol.MIN_WIDTH || width > DefSymbol.MAX_WIDTH) {
                    widthValid = false;
                }
            }

            // TODO(maybe): do this the XAML way, with properties and Styles
            labelNotesLabel.Foreground = labelValid ? mDefaultLabelColor : Brushes.Red;
            labelUniqueLabel.Foreground = projectLabelUniqueLabel.Foreground =
                labelUnique ? mDefaultLabelColor : Brushes.Red;
            valueNotesLabel.Foreground = valueValid ? mDefaultLabelColor : Brushes.Red;
            widthNotesLabel.Foreground = widthValid ? mDefaultLabelColor : Brushes.Red;

            IsValid = labelValid && labelUnique && valueValid && widthValid;
        }

        private bool ParseValue(out int value, out int numBase) {
            string str = Value;
            if (str.IndexOf('/') >= 0) {
                // treat as address
                numBase = 16;
                return Asm65.Address.ParseAddress(str, (1 << 24) - 1, out value);
            } else {
                return Asm65.Number.TryParseInt(str, out value, out numBase);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            bool isConstant = (constantRadioButton.IsChecked == true);

            ParseValue(out int value, out int numBase);
            FormatDescriptor.SubType subType = FormatDescriptor.GetSubTypeForBase(numBase);
            int width = DefSymbol.NO_WIDTH;
            if (!string.IsNullOrEmpty(VarWidth)) {
                width = int.Parse(VarWidth);
            }

            NewSym = new DefSymbol(Label, value, Symbol.Source.Project,
                isConstant ? Symbol.Type.Constant : Symbol.Type.ExternalAddr,
                subType, Comment, string.Empty, width);

            DialogResult = true;
        }
    }
}
