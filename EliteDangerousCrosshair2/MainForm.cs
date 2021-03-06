﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace EliteDangerousCrosshair2
{
    public sealed partial class MainForm : Form
    {
        private readonly InvisibleForm _invisibleForm;
        private readonly List<IntPtr> _thisId;
        private bool _activeGameUi;
        private IntPtr _currentId;
        private IntPtr _gameId;
        private WindowLocation _windowLocation;
        private readonly KeyboardHook _hook = new KeyboardHook();
        private bool _activate;

        public new bool Activate
        {
            get { return _activate; }
            set
            {
                if (_activate != value)
                {
                    _activate = value;
                    _invisibleForm.SetActivate(Activate);
                }
            }
        }


        public MainForm()
        {
            InitializeComponent();

            if (!IsHandleCreated) CreateHandle();

            _thisId = new List<IntPtr>();

            _invisibleForm = new InvisibleForm();

            AddThisId(_invisibleForm.Handle);
            Activate = true;


            var monitorFocus = new Thread(MonitorFocus) {IsBackground = true};
            monitorFocus.Start();

            var monitorGame = new Thread(MonitorGameStatus) {IsBackground = true};
            monitorGame.Start();

            AddThisId(Process.GetCurrentProcess().MainWindowHandle);
            AddThisId(Handle);


            _hook.KeyPressed +=
                hook_KeyPressed;
            // register the control + shift + F12 combination as hot key.
            _hook.RegisterHotKey((ModifierKeys) 2 | (ModifierKeys) 4, Keys.F1);
            InitComboBox();
        }

        private void InitComboBox()
        {
            for (int i = 1; i <= 10; i++)
            {
                comboBoxLineThickness.Items.Add(i.ToString());
            }

            comboBoxLineThickness.SelectedIndex = (int)Properties.Settings.Default["LineThicknessIndex"];

            for (int i = 0; i <= 100; i += 10)
            {
                comboBoxOpacity.Items.Add(i.ToString() + "%");
            }

            comboBoxOpacity.SelectedIndex = (int) Properties.Settings.Default["OpacityIndex"];
        }

        private void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            // show the keys pressed in a label.
            Activate = !Activate;
        }

        private WindowLocation GameWindowLocation
        {
            get { return _windowLocation; }
            set
            {
                if (_windowLocation == null || !_windowLocation.Equals(value))
                {
                    _windowLocation = value;
                    _invisibleForm.GameWindowLocation = GameWindowLocation;
                }
            }
        }

        public bool ActiveGameUi
        {
            get { return _activeGameUi; }
            set
            {
                if (_activeGameUi != value)
                {
                    _activeGameUi = value;
                    //Invoke(new MethodInvoker(delegate { label1.Text = value.ToString(); }));
                    _invisibleForm.ShowScreen = value;
                }
            }
        }


        public IntPtr GameId
        {
            get { return _gameId; }
            set
            {
                if (_gameId != value)
                {
                    _gameId = value;
                    Invoke(_gameId.ToInt32() == 0
                        ? new MethodInvoker(delegate
                        {
                            labelGameStatus.Text = "Game Not Found!";
                            labelGameStatus.ForeColor = ForeColor;
                        })
                        : delegate
                        {
                            labelGameStatus.Text = "Game Found!";
                            labelGameStatus.ForeColor = Color.Green;
                            _invisibleForm.Hide();
                        });
                }
            }
        }

        private IntPtr CurrentId
        {
            set
            {
                if (_currentId != value)
                {
                    _currentId = value;

                    if (((value == GameId) || _thisId.Contains(value)) && GameId.ToInt32() != 0)
                    {
                        ActiveGameUi = true;
                        _invisibleForm.GameId = GameId;
                        _invisibleForm.ProgramFocus = true;
                    }
                    else
                    {
                        ActiveGameUi = false;
                        _invisibleForm.ProgramFocus = false;
                    }
                }
            }
        }

        private void AddThisId(IntPtr add)
        {
            if (!_thisId.Contains(add))
            {
                _thisId.Add(add);
            }
        }

        private void MonitorFocus()
        {
            while (true)
            {
                CurrentId = GameMonitor.GetForegroundWindow();
                Thread.Sleep(100);
            }
        }

        private void MonitorGameStatus()
        {
            while (true)
            {
                var ed32 = GameMonitor.GetSingleProcessByName("EliteDangerous32");
                var ed64 = GameMonitor.GetSingleProcessByName("EliteDangerous64");

                if (ed32.ToInt32() > 0)
                {
                    GameId = ed32;
                } else
                {
                    GameId = ed64;
                }
                if (GameId.ToInt32() != 0)
                {
                    var wl = new WindowLocation {Rect = GameMonitor.GetRect(GameId)};
                    GameMonitor.ClientToScreen(GameId, ref wl.TopPoint);
                    if (GameWindowLocation != null)
                    {
                        if (!wl.Equals(GameWindowLocation))
                        {
                            GameWindowLocation = wl;
                        }
                    }
                    else
                    {
                        GameWindowLocation = wl;
                    }
                }
                Thread.Sleep(25);
            }
        }

        public void MonitorGameWindow()
        {
            while (true)
            {
                if (!ActiveGameUi) continue;
                var wl = new WindowLocation {Rect = GameMonitor.GetRect(GameId)};
                GameMonitor.ClientToScreen(GameId, ref wl.TopPoint);
                if (GameWindowLocation != null)
                {
                    if (!wl.Equals(GameWindowLocation))
                    {
                        GameWindowLocation = wl;
                    }
                }
                else
                {
                    GameWindowLocation = wl;
                }
                Thread.Sleep(500);
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            int size = trackBar1.Value*5;
            label1.Text = "Reticle Size: " + size + "px";
            _invisibleForm.CircleDem = size;
            Properties.Settings.Default["ReticleSizeIndex"] = size;
            Properties.Settings.Default.Save();
        }

        private void linkUpdateLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            const string url = "https://github.com/RobCubed/EliteDangerousCrosshair/releases";
            try
            {
                Process.Start(url);
            }
            catch (Win32Exception)
            {
                Process.Start(url);
            }
        }

        private void comboBoxLineThickness_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxLineThickness.SelectedItem != null)
            {
                _invisibleForm.SetLineThickness(Convert.ToInt32(comboBoxLineThickness.SelectedItem));
                Properties.Settings.Default["LineThicknessIndex"] = comboBoxLineThickness.SelectedIndex;
                Properties.Settings.Default.Save();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = colorDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                panel1.BackColor = colorDialog1.Color;
                _invisibleForm.SetColor(colorDialog1.Color);
                Properties.Settings.Default["ReticleColor"] = colorDialog1.Color;
                Properties.Settings.Default.Save();
            }
        }

        private void comboBoxOpacity_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxOpacity.SelectedItem != null)
            {
                int i = Convert.ToInt32(comboBoxOpacity.SelectedItem.ToString().TrimEnd('%'));
                double d = i/100.0;
                _invisibleForm.SetOpacity(d);
                Properties.Settings.Default["OpacityIndex"] = comboBoxOpacity.SelectedIndex;
                Properties.Settings.Default.Save();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxNubs.Checked)
            {
                _invisibleForm.EnableNubs(true);
            }
            else
            {
                _invisibleForm.EnableNubs(false);
            }
        }

    }
}