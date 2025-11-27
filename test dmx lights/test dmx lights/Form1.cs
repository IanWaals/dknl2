using System;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;

namespace DMXControl
{
    public partial class Form1 : Form
    {
        SerialPort dmxPort;
        byte[] dmxData = new byte[512]; // DMX has 512 channels
        Thread dmxThread;
        bool isRunning = false;

        private byte[] savedDmxData = new byte[9];
        bool lightsTogglePower = true;

        public Form1()
        {
            InitializeComponent();
            dmxPort = new SerialPort("COM13", 250000, Parity.None, 8, StopBits.Two);

        }

        private void Form1_Load(object sender, EventArgs e)
        {
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

        private void trbChannel1Iwaa_Scroll(object sender, EventArgs e)
        {
            // Update DMX channel 1 (array index 0)
            dmxData[0] = (byte)trbChannel1iwaa.Value;
            savedDmxData[0] = (byte)dmxData[0];

            // Update the label to show current value
            if (lblChannel1Value != null)
            {
                lblChannel1Value.Text = $"Channel 1: {dmxData[0]}";
            }

            // No need to call SendDMX() here - the background thread handles it
        }

        private void trbChannel2_Scroll(object sender, EventArgs e)
        {
            dmxData[1] = (byte)trbChannel2iwaa.Value;
            savedDmxData[1] =(byte)dmxData[1];

            if (lblChannel2Value != null)
            {
                lblChannel2Value.Text = $"Channel 2: {dmxData[1]}";
            }
        }

        private void trbChannel3iwaa_Scroll(object sender, EventArgs e)
        {
            dmxData[2] = (byte)trbChannel3iwaa.Value;
            savedDmxData[2] = (byte)dmxData[2];

            if (lblChannel3Value != null)
            {
                lblChannel3Value.Text = $"Channel 3: {dmxData[2]}";
            }
        }

        private void btnTogglePowerIwaa_Click(object sender, EventArgs e)
        {
            if (lightsTogglePower)
            {
                dmxData[0] = 0;
                dmxData[1] = 0;
                dmxData[2] = 0;

                trbChannel1iwaa.Enabled = false;
                trbChannel2iwaa.Enabled = false;
                trbChannel3iwaa.Enabled = false;

                btnTogglePowerIwaa.Text = "turned off";
                lightsTogglePower = false;
            } else
            {
                dmxData[0] = (byte)savedDmxData[0];
                dmxData[1] = (byte)savedDmxData[1];
                dmxData[2] = (byte)savedDmxData[2];

                trbChannel1iwaa.Enabled = true;
                trbChannel2iwaa.Enabled = true;
                trbChannel3iwaa.Enabled = true;

                btnTogglePowerIwaa.Text = "turned on";
                lightsTogglePower = true;

                SendDMXFrame();
            }
        }
    }
}