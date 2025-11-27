using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Clubbing_for_coders
{
    public partial class Form1 : Form
    {
        SerialPort dmxPort;
        byte[] dmxData = new byte[512]; // DMX has 512 channels
        Thread dmxThread;
        bool isRunning = false;

        private byte[] savedDmxData = new byte[9];
        bool lightsTogglePower = true;
        bool toggleFlashing = false;

        public Form1()
        {
            InitializeComponent();
            dmxPort = new SerialPort("COM8", 250000, Parity.None, 8, StopBits.Two);
        }

        private void btnOpenControllerIwaa_Click(object sender, EventArgs e)
        {
            tbcPagesIwaa.SelectedIndex = 1;
        }

        private void btnOpenShowsIwaa_Click(object sender, EventArgs e)
        {
            tbcPagesIwaa.SelectedIndex = 2;
        }

        private void btnOpenTimelineIwaa_Click(object sender, EventArgs e)
        {
            tbcPagesIwaa.SelectedIndex = 3;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            tbcPagesIwaa.Appearance = TabAppearance.FlatButtons;
            tbcPagesIwaa.ItemSize = new Size(0, 1);
            tbcPagesIwaa.SizeMode = TabSizeMode.Fixed;
            trbFlashIntervalIwaa.Enabled = false;

            dmxSliderRedRoli.ValueChanged += v => dmxData[0] = (byte)(v * 21);
            dmxSliderGreenRoli.ValueChanged += v => dmxData[1] = (byte)(v * 21);
            dmxSliderBlueRoli.ValueChanged += v => dmxData[2] = (byte)(v * 21);

            // create the toggle switch
            ToggleSwitch togglePower = new ToggleSwitch();
            togglePower.Location = btnTogglePowerAllParIwaa.Location;

            // connect to your existing DMX ON/OFF logic
            togglePower.Toggled += TogglePower_Toggled;

            // add to UI
            tbpDmxControllerIwaa.Controls.Add(togglePower);
            togglePower.BringToFront();

            // hide unused button
            btnTogglePowerAllParIwaa.Visible = false;

            // ensure lights actually START OFF in DMX:
            dmxData[0] = 0;
            dmxData[1] = 0;
            dmxData[2] = 0;
            lightsTogglePower = false;

            try
            {
                dmxPort.Open();

                // Start the continuous DMX transmission thread
                isRunning = true;
                dmxThread = new Thread(DMXTransmissionLoop);
                dmxThread.IsBackground = true;
                dmxThread.Start();

                MessageBox.Show("DMX Port Opened and transmission started!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening DMX port: " + ex.Message);
            }
            dmxSliderRedRoli.Enabled = lightsTogglePower;
            dmxSliderGreenRoli.Enabled = lightsTogglePower;
            dmxSliderBlueRoli.Enabled = lightsTogglePower;
        }

        private void DMXTransmissionLoop()
        {
            while (isRunning && dmxPort.IsOpen)
            {
                try
                {
                    SendDMXFrame();
                    Thread.Sleep(25); // ~40Hz refresh rate (typical for DMX)
                }
                catch (Exception ex)
                {
                    // Log error but continue trying
                    Console.WriteLine("DMX transmission error: " + ex.Message);
                }
            }
        }

        private void SendDMXFrame()
        {
            if (!dmxPort.IsOpen) return;

            try
            {
                // Generate BREAK by temporarily switching to slower baud rate
                dmxPort.BaudRate = 80000;
                dmxPort.Write(new byte[] { 0 }, 0, 1);

                // Return to normal DMX baud rate
                dmxPort.BaudRate = 250000;

                // Send START code (0x00)
                dmxPort.Write(new byte[] { 0 }, 0, 1);

                // Send all 512 DMX channel values
                dmxPort.Write(dmxData, 0, dmxData.Length);

                // Wait for transmission to complete
                while (dmxPort.BytesToWrite != 0) ;
                Thread.Sleep(20);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in SendDMXFrame: " + ex.Message);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop the transmission thread
            isRunning = false;

            if (dmxThread != null && dmxThread.IsAlive)
            {
                dmxThread.Join(1000); // Wait up to 1 second for thread to finish
            }

            // Turn off all channels before closing
            Array.Clear(dmxData, 0, dmxData.Length);
            if (dmxPort.IsOpen)
            {
                SendDMXFrame();
                Thread.Sleep(50);
                dmxPort.Close();
            }
        }

        private void TogglePower_Toggled(object sender, EventArgs e)
        {
            // Call your existing power toggle logic
            btnTogglePowerAllParIwaa_Click(sender, e);

            // Make sure sliders reflect current state
            dmxSliderRedRoli.Enabled = lightsTogglePower;
            dmxSliderGreenRoli.Enabled = lightsTogglePower;
            dmxSliderBlueRoli.Enabled = lightsTogglePower;
        }


        private void btnTogglePowerAllParIwaa_Click(object sender, EventArgs e)
        {
            if (lightsTogglePower)
            {
                // Turn off flashing first if active
                if (toggleFlashing)
                {
                    toggleFlashing = false;
                    btnFlashLightsIwaa.Text = "Make the lights flash";
                    trbFlashIntervalIwaa.Enabled = false;
                }

                // TURN LIGHTS OFF
                dmxData[0] = 0;
                dmxData[1] = 0;
                dmxData[2] = 0;

                // RESET SLIDERS
                dmxSliderRedRoli.Value = 0;
                dmxSliderGreenRoli.Value = 0;
                dmxSliderBlueRoli.Value = 0;

                // DISABLE SLIDERS WHEN OFF
                dmxSliderRedRoli.Enabled = false;
                dmxSliderGreenRoli.Enabled = false;
                dmxSliderBlueRoli.Enabled = false;

                lightsTogglePower = false;
            }
            else
            {
                // TURN LIGHTS ON
                dmxData[0] = (byte)savedDmxData[0];
                dmxData[1] = (byte)savedDmxData[1];
                dmxData[2] = (byte)savedDmxData[2];

                // RESTORE SLIDER POSITIONS
                dmxSliderRedRoli.Value = savedDmxData[0] / 21;
                dmxSliderGreenRoli.Value = savedDmxData[1] / 21;
                dmxSliderBlueRoli.Value = savedDmxData[2] / 21;

                // ENABLE SLIDERS WHEN ON
                dmxSliderRedRoli.Enabled = true;
                dmxSliderGreenRoli.Enabled = true;
                dmxSliderBlueRoli.Enabled = true;

                lightsTogglePower = true;

                SendDMXFrame();
            }
        }

        private void btnFlashLightsIwaa_Click(object sender, EventArgs e)
        {
            if (toggleFlashing == false)
            {
                toggleFlashing = true;
                btnFlashLightsIwaa.Text = "Stop Flashing";
                trbFlashIntervalIwaa.Enabled = true;
                LightsFlashing();
            }
            else
            {
                toggleFlashing = false;
                btnFlashLightsIwaa.Text = "Make the lights flash";
                trbFlashIntervalIwaa.Enabled = false;
            }
        }

        private async void LightsFlashing()
        {
            // Store the current RGB values from trackbars
            byte flashRed = (byte)(dmxSliderRedRoli.Value * 21);
            byte flashGreen = (byte)(dmxSliderGreenRoli.Value * 21);
            byte flashBlue = (byte)(dmxSliderBlueRoli.Value * 21);

            // Disable trackbars during flashing
            dmxSliderRedRoli.Enabled = false;
            dmxSliderGreenRoli.Enabled = false;
            dmxSliderBlueRoli.Enabled = false;

            // Change button text to indicate flashing is active
            btnFlashLightsIwaa.Text = "Flashing... (Click to Stop)";

            // Flash loop - runs until toggleFlashing is set to false
            while (toggleFlashing)
            {
                // Turn lights ON with current colors
                dmxData[0] = flashRed;
                dmxData[1] = flashGreen;
                dmxData[2] = flashBlue;

                await Task.Delay(trbFlashIntervalIwaa.Value); // Wait (lights on)

                if (!toggleFlashing) break; // Check if stopped

                // Turn lights OFF
                dmxData[0] = 0;
                dmxData[1] = 0;
                dmxData[2] = 0;

                await Task.Delay(trbFlashIntervalIwaa.Value); // Wait (lights off)
            }

            // Reset button
            btnFlashLightsIwaa.Text = "Make the lights flash";
            btnFlashLightsIwaa.Click += btnFlashLightsIwaa_Click;

            // Enable trackbars during flashing
            dmxSliderRedRoli.Enabled = true;
            dmxSliderGreenRoli.Enabled = true;
            dmxSliderBlueRoli.Enabled = true;
        }

        private void trbFlashIntervalIwaa_Scroll(object sender, EventArgs e)
        {
            lblFlashIntervalIwaa.Text = trbFlashIntervalIwaa.Value.ToString();
        }
    }
}
