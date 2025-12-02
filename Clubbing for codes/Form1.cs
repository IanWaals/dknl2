using Dmx512UsbRs485;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Text;
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

        private bool isPlayingShow = false;
        private CancellationTokenSource playShowCancellationToken;

        string connectionString = @"Data Source=localhost\sqlexpress;Initial Catalog=FunDatabasename;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";

        private Dictionary<Panel, string> timelineSlots = new Dictionary<Panel, string>();

        private System.Windows.Forms.Timer dmxUpdateTimer;

        //MH variables
        private Dmx512UsbRs485Driver driver;
        int tilt1 = 0;
        int tilt2 = 0;
        int tilt3 = 0;
        int tilt4 = 0;
        int pan1 = 0;
        int pan2 = 0;
        int pan3 = 0;
        int pan4 = 0;
        int red = 0;
        int blue = 0;
        int green = 0;
        int white = 0;
        int speed = 128;
        int dim1 = 255;
        int dim2 = 255;
        int dim3 = 255;
        int dim4 = 255;
        int strobe = 0;

        private void InitializeTimeline()
        {
            // Initialize function panels
            SetupFunctionPanel(pnlTurnRedFunctionIwaa, "turnRed", Color.Red);
            SetupFunctionPanel(pnlTurnGreenFunctionIwaa, "turnGreen", Color.Green);
            SetupFunctionPanel(pnlTurnBlueFunctionIwaa, "turnBlue", Color.Blue);
            SetupFunctionPanel(pnlTurnWhiteFunctionIwaa, "turnWhite", Color.White);
            SetupFunctionPanel(pnlTurnPurpleFunctionIwaa, "turnPurple", Color.Purple);
            SetupFunctionPanel(pnlTurnOrangeFunctionIwaa, "turnOrange", Color.Orange);
            SetupFunctionPanel(pnlTurnYellowFunctionIwaa, "turnYellow", Color.Yellow);
            SetupFunctionPanel(pnlTurnOnFunctionIwaa, "turnOn", Color.LightGreen);
            SetupFunctionPanel(pnlTurnOffFunctionIwaa, "turnOff", Color.Gray);
            SetupFunctionPanel(pnlStartFlashingFunctionIwaa, "startFlashing", Color.Pink);
            SetupFunctionPanel(pnlPanFunctionIwaa, "pan", Color.LightBlue);
            SetupFunctionPanel(pnlTiltFunctionIwaa, "tilt", Color.LightCoral);

            // Initialize all timeline slots with their textboxes
            InitializeTimelineSlot(pnlTimelineSlot1, txbFunctionDuration1, txbAngle1);
            InitializeTimelineSlot(pnlTimelineSlot2, txbFunctionDuration2, txbAngle2);
            InitializeTimelineSlot(pnlTimelineSlot3, txbFunctionDuration3, txbAngle3);
            InitializeTimelineSlot(pnlTimelineSlot4, txbFunctionDuration4, txbAngle4);
            InitializeTimelineSlot(pnlTimelineSlot5, txbFunctionDuration5, txbAngle5);
            InitializeTimelineSlot(pnlTimelineSlot6, txbFunctionDuration6, txbAngle6);
            InitializeTimelineSlot(pnlTimelineSlot7, txbFunctionDuration7, txbAngle7);
            InitializeTimelineSlot(pnlTimelineSlot8, txbFunctionDuration8, txbAngle8);
            InitializeTimelineSlot(pnlTimelineSlot9, txbFunctionDuration9, txbAngle9);
            InitializeTimelineSlot(pnlTimelineSlot10, txbFunctionDuration10, txbAngle10);
            InitializeTimelineSlot(pnlTimelineSlot11, txbFunctionDuration11, txbAngle11);
            InitializeTimelineSlot(pnlTimelineSlot12, txbFunctionDuration12, txbAngle12);
            InitializeTimelineSlot(pnlTimelineSlot13, txbFunctionDuration13, txbAngle13);
            InitializeTimelineSlot(pnlTimelineSlot14, txbFunctionDuration14, txbAngle14);
            InitializeTimelineSlot(pnlTimelineSlot15, txbFunctionDuration15, txbAngle15);
            InitializeTimelineSlot(pnlTimelineSlot16, txbFunctionDuration16, txbAngle16);
            InitializeTimelineSlot(pnlTimelineSlot17, txbFunctionDuration17, txbAngle17);

            // Setup save button event
            btnSaveShowIwaa.Click += btnSaveShowIwaa_Click;
        }

        private void SetupFunctionPanel(Panel panel, string functionName, Color color)
        {
            panel.BackColor = color;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.AllowDrop = false;
            panel.Tag = functionName;
            panel.Cursor = Cursors.Hand;

            // Add label to show function name
            Label lbl = new Label
            {
                Text = GetFriendlyName(functionName),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 7, FontStyle.Bold),
                ForeColor = (color == Color.Yellow || color == Color.White || color == Color.LightGreen)
                    ? Color.Black : Color.White
            };

            // IMPORTANT: Make the label pass mouse events through to the panel
            lbl.MouseDown += FunctionPanel_MouseDown;

            panel.Controls.Add(lbl);

            // Mouse events for dragging
            panel.MouseDown += FunctionPanel_MouseDown;
        }

        private void InitializeTimelineSlot(Panel panel, TextBox durationTextBox, TextBox angleTextBox)
        {
            panel.AllowDrop = true;
            panel.BorderStyle = BorderStyle.Fixed3D;
            panel.BackColor = Color.WhiteSmoke;

            // Initialize with empty string
            timelineSlots[panel] = "";

            // Store references to textboxes in the panel's Tag
            panel.Tag = new { DurationTextBox = durationTextBox, AngleTextBox = angleTextBox };

            // Initially disable textboxes
            durationTextBox.Enabled = false;
            angleTextBox.Enabled = false;
            durationTextBox.Text = "2"; // Default duration

            // Drag events
            panel.DragEnter += TimelineSlot_DragEnter;
            panel.DragDrop += TimelineSlot_DragDrop;

            // Right-click to clear
            panel.MouseClick += TimelineSlot_MouseClick;
        }

        private void FunctionPanel_MouseDown(object sender, MouseEventArgs e)
        {
            // Handle both panel and label clicks
            Panel panel = sender as Panel;

            // If sender is a label, get its parent panel
            if (panel == null && sender is Label)
            {
                panel = ((Label)sender).Parent as Panel;
            }

            if (panel != null && panel.Tag != null)
            {
                DataObject data = new DataObject(DataFormats.Text, panel.Tag.ToString());
                panel.DoDragDrop(data, DragDropEffects.Copy);
            }
        }

        private void TimelineSlot_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void TimelineSlot_DragDrop(object sender, DragEventArgs e)
        {
            Panel slot = sender as Panel;
            if (slot != null && e.Data.GetDataPresent(DataFormats.Text))
            {
                string functionName = e.Data.GetData(DataFormats.Text).ToString();

                // Store the function in the slot
                timelineSlots[slot] = functionName;

                // Update visual appearance
                UpdateSlotAppearance(slot, functionName);

                // Enable/disable textboxes based on function type
                UpdateTextBoxStates(slot, functionName);
            }
        }

        private void TimelineSlot_MouseClick(object sender, MouseEventArgs e)
        {
            // Right-click to clear a slot
            if (e.Button == MouseButtons.Right)
            {
                Panel slot = sender as Panel;
                if (slot != null)
                {
                    timelineSlots[slot] = "";
                    slot.BackColor = Color.WhiteSmoke;
                    slot.Controls.Clear();

                    // Disable textboxes
                    var textBoxes = slot.Tag as dynamic;
                    if (textBoxes != null)
                    {
                        textBoxes.DurationTextBox.Enabled = false;
                        textBoxes.AngleTextBox.Enabled = false;
                        textBoxes.DurationTextBox.Text = "2";
                        textBoxes.AngleTextBox.Text = "";
                    }
                }
            }
        }

        private void UpdateTextBoxStates(Panel slot, string functionName)
        {
            var textBoxes = slot.Tag as dynamic;
            if (textBoxes != null)
            {
                TextBox durationBox = textBoxes.DurationTextBox;
                TextBox angleBox = textBoxes.AngleTextBox;

                // Enable duration textbox for all functions
                durationBox.Enabled = true;
                if (string.IsNullOrEmpty(durationBox.Text))
                {
                    durationBox.Text = "2"; // Default duration
                }

                // Enable angle textbox only for pan and tilt
                if (functionName.ToLower() == "pan" || functionName.ToLower() == "tilt")
                {
                    angleBox.Enabled = true;
                    if (string.IsNullOrEmpty(angleBox.Text))
                    {
                        angleBox.Text = "0";
                    }
                }
                else
                {
                    angleBox.Enabled = false;
                    angleBox.Text = "";
                }
            }
        }

        private void UpdateSlotAppearance(Panel slot, string functionName)
        {
            // Clear existing controls
            slot.Controls.Clear();

            // Set background color based on function
            slot.BackColor = GetFunctionColor(functionName);

            // Add label
            Label lbl = new Label
            {
                Text = GetFriendlyName(functionName),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 6, FontStyle.Bold),
                ForeColor = (functionName.Contains("Yellow") || functionName.Contains("White") || functionName.Contains("On"))
                    ? Color.Black : Color.White
            };
            slot.Controls.Add(lbl);
        }

        private Color GetFunctionColor(string functionName)
        {
            switch (functionName.ToLower())
            {
                case "turnred": return Color.Red;
                case "turngreen": return Color.Green;
                case "turnblue": return Color.Blue;
                case "turnwhite": return Color.White;
                case "turnpurple": return Color.Purple;
                case "turnorange": return Color.Orange;
                case "turnyellow": return Color.Yellow;
                case "turnon": return Color.LightGreen;
                case "turnoff": return Color.Gray;
                case "startflashing": return Color.Pink;
                case "pan": return Color.LightBlue;
                case "tilt": return Color.LightCoral;
                default: return Color.WhiteSmoke;
            }
        }

        private string GetFriendlyName(string functionName)
        {
            switch (functionName.ToLower())
            {
                case "turnred": return "Red";
                case "turngreen": return "Green";
                case "turnblue": return "Blue";
                case "turnwhite": return "White";
                case "turnpurple": return "Purple";
                case "turnorange": return "Orange";
                case "turnyellow": return "Yellow";
                case "turnon": return "On";
                case "turnoff": return "Off";
                case "startflashing": return "Flash";
                case "pan": return "Pan";
                case "tilt": return "Tilt";
                default: return functionName;
            }
        }

        private void btnSaveShowIwaa_Click(object sender, EventArgs e)
        {
            // Get all timeline panels in order
            List<Panel> orderedSlots = new List<Panel>
            {
                pnlTimelineSlot1, pnlTimelineSlot2, pnlTimelineSlot3, pnlTimelineSlot4,
                pnlTimelineSlot5, pnlTimelineSlot6, pnlTimelineSlot7, pnlTimelineSlot8,
                pnlTimelineSlot9, pnlTimelineSlot10, pnlTimelineSlot11, pnlTimelineSlot12,
                pnlTimelineSlot13, pnlTimelineSlot14, pnlTimelineSlot15, pnlTimelineSlot16,
                pnlTimelineSlot17
            };

            // Build the function sequence string
            List<string> functions = new List<string>();
            foreach (Panel slot in orderedSlots)
            {
                if (timelineSlots.ContainsKey(slot) && !string.IsNullOrEmpty(timelineSlots[slot]))
                {
                    string functionName = timelineSlots[slot];
                    var textBoxes = slot.Tag as dynamic;

                    if (textBoxes != null)
                    {
                        TextBox durationBox = textBoxes.DurationTextBox;
                        TextBox angleBox = textBoxes.AngleTextBox;

                        string duration = string.IsNullOrEmpty(durationBox.Text) ? "2" : durationBox.Text;

                        // For pan and tilt, include angle parameter
                        if (functionName.ToLower() == "pan" || functionName.ToLower() == "tilt")
                        {
                            string angle = string.IsNullOrEmpty(angleBox.Text) ? "0" : angleBox.Text;
                            functions.Add($"{functionName}:{angle}:{duration}");
                        }
                        else
                        {
                            functions.Add($"{functionName}{duration}");
                        }
                    }
                    else
                    {
                        functions.Add($"{functionName}2"); // Default duration if textbox not found
                    }
                }
            }

            if (functions.Count == 0)
            {
                MessageBox.Show("Please add at least one function to the timeline before saving.");
                return;
            }

            // Join functions with comma separator
            string functionSequence = string.Join(", ", functions);

            // Prompt for show name
            string showName = PromptForShowName();
            if (string.IsNullOrWhiteSpace(showName))
            {
                return; // User cancelled
            }

            // Save to database
            SaveShowToDatabase(showName, functionSequence);
        }

        private string PromptForShowName()
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Save Show",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label textLabel = new Label() { Left = 20, Top = 20, Text = "Enter show name:", AutoSize = true };
            TextBox textBox = new TextBox() { Left = 20, Top = 45, Width = 340 };
            Button confirmation = new Button() { Text = "Save", Left = 200, Width = 80, Top = 75, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 290, Width = 70, Top = 75, DialogResult = DialogResult.Cancel };

            confirmation.Click += (sender, e) => { prompt.Close(); };
            cancel.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }

        private void SaveShowToDatabase(string showName, string functionSequence)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO savedShows (name, functionSequence) VALUES (@name, @functionSequence)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", showName);
                        cmd.Parameters.AddWithValue("@functionSequence", functionSequence);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show($"Show '{showName}' saved successfully!\n\nSequence: {functionSequence}",
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Reload the combo box on the saved shows page
                            LoadComboBox();

                            // Optionally clear the timeline
                            if (MessageBox.Show("Do you want to clear the timeline?", "Clear Timeline",
                                MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                ClearTimeline();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving show to database: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearTimeline()
        {
            foreach (var slot in timelineSlots.Keys.ToList())
            {
                timelineSlots[slot] = "";
                slot.BackColor = Color.WhiteSmoke;
                slot.Controls.Clear();
            }
        }

        public Form1()
        {
            InitializeComponent();
            //dmxPort = new SerialPort("COM12", 250000, Parity.None, 8, StopBits.Two);

            driver = new Dmx512UsbRs485Driver();
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

        private void Form1_Load(object sender, EventArgs e) {

            // Your existing code
            // Reset toggle switches
            cbxSwitc1Iwaa.Checked = true;
            cbxSwitch2Iwaa.Checked = true;
            cbxSwitch3Iwaa.Checked = true;
            cbxSwitch4Iwaa.Checked = true;

            LoadComboBox();
            InitializeTimeline();
            tbcPagesIwaa.Appearance = TabAppearance.FlatButtons;
            tbcPagesIwaa.ItemSize = new Size(0, 1);
            tbcPagesIwaa.SizeMode = TabSizeMode.Fixed;
            trbFlashIntervalIwaa.Enabled = false;

            try
            {
                driver.DmxToDefault("COM12");

                for (int i = 1; i <= 512; i++)
                {
                    driver.DmxLoadBuffer(i, 0, 512);
                }

                dmxUpdateTimer = new System.Windows.Forms.Timer();
                dmxUpdateTimer.Interval = 25;
                dmxUpdateTimer.Tick += DmxUpdateTimer_Tick;
                dmxUpdateTimer.Start();

                MessageBox.Show("DMX Port Opened and transmission started!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening DMX port: " + ex.Message);
            }
        }

        private void DmxUpdateTimer_Tick(object sender, EventArgs e)
        {
            // Update PAR lights (channels 1-9 for 3 lights)
            // Each PAR light uses 3 channels: R, G, B
            driver.DmxLoadBuffer(1, dmxData[0], 512); // PAR 1 Red
            driver.DmxLoadBuffer(2, dmxData[1], 512); // PAR 1 Green
            driver.DmxLoadBuffer(3, dmxData[2], 512); // PAR 1 Blue

            driver.DmxLoadBuffer(1, dmxData[0], 512); // PAR 2 Red (same as PAR 1)
            driver.DmxLoadBuffer(2, dmxData[1], 512); // PAR 2 Green
            driver.DmxLoadBuffer(3, dmxData[2], 512); // PAR 2 Blue

            driver.DmxLoadBuffer(1, dmxData[0], 512); // PAR 3 Red (same as PAR 1)
            driver.DmxLoadBuffer(2, dmxData[1], 512); // PAR 3 Green
            driver.DmxLoadBuffer(3, dmxData[2], 512); // PAR 3 Blue

            // Update Moving Heads (channels 21-39)
            driver.DmxLoadBuffer(21, 255, 512);        // Master dimmer
            driver.DmxLoadBuffer(22, (byte)strobe, 512); // Strobe
            driver.DmxLoadBuffer(23, (byte)red, 512);    // Red
            driver.DmxLoadBuffer(24, (byte)green, 512);  // Green
            driver.DmxLoadBuffer(25, (byte)blue, 512);   // Blue
            driver.DmxLoadBuffer(26, (byte)white, 512);  // White

            // Head 1
            driver.DmxLoadBuffer(27, (byte)pan1, 512);
            driver.DmxLoadBuffer(28, (byte)tilt1, 512);
            driver.DmxLoadBuffer(29, (byte)dim1, 512);

            // Head 2
            driver.DmxLoadBuffer(30, (byte)pan2, 512);
            driver.DmxLoadBuffer(31, (byte)tilt2, 512);
            driver.DmxLoadBuffer(32, (byte)dim2, 512);

            // Head 3
            driver.DmxLoadBuffer(33, (byte)pan3, 512);
            driver.DmxLoadBuffer(34, (byte)tilt3, 512);
            driver.DmxLoadBuffer(35, (byte)dim3, 512);

            // Head 4
            driver.DmxLoadBuffer(36, (byte)pan4, 512);
            driver.DmxLoadBuffer(37, (byte)tilt4, 512);
            driver.DmxLoadBuffer(38, (byte)dim4, 512);

            // Speed
            driver.DmxLoadBuffer(39, (byte)speed, 512);

            // Send all data (send full 512 channels for reliability)
            driver.DmxSendCommand(512);
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
            // Stop the timer
            if (dmxUpdateTimer != null)
            {
                dmxUpdateTimer.Stop();
                dmxUpdateTimer.Dispose();
            }

            // Clear all channels
            for (int i = 1; i <= 512; i++)
            {
                driver.DmxLoadBuffer(i, 0, 512);
            }
            driver.DmxSendCommand(512);

            // Give it time to send
            System.Threading.Thread.Sleep(100);
        }

        private void btnTogglePowerAllParIwaa_Click(object sender, EventArgs e)
        {
            if (lightsTogglePower)
            {
                turnOff();
            }
            else
            {
                turnOn();
            }
        }

        private void trbRedParIwaa_Scroll(object sender, EventArgs e)
        {
            // Update DMX channel 1 (array index 0)
            dmxData[0] = (byte)trbRedParIwaa.Value;
            savedDmxData[0] = (byte)dmxData[0];

            // No need to call SendDMX() here - the background thread handles it
        }

        private void trbGreenParIwaa_Scroll(object sender, EventArgs e)
        {
            // Update DMX channel 1 (array index 0)
            dmxData[1] = (byte)trbGreenParIwaa.Value;
            savedDmxData[1] = (byte)dmxData[1];

            // No need to call SendDMX() here - the background thread handles it
        }

        private void trbBlueParIwaa_Scroll(object sender, EventArgs e)
        {
            // Update DMX channel 1 (array index 0)
            dmxData[2] = (byte)trbBlueParIwaa.Value;
            savedDmxData[2] = (byte)dmxData[2];

            // No need to call SendDMX() here - the background thread handles it
        }

        private void btnFlashLightsIwaa_Click(object sender, EventArgs e)
        {
            if (toggleFlashing == false)
            {
                startFlashing();
            }
            else
            {
                stopFlashing();
            }
        }

        private async void LightsFlashing()
        {
            disableButtons();
            // Store the current RGB values from trackbars
            byte flashRed = (byte)trbRedParIwaa.Value;
            byte flashGreen = (byte)trbGreenParIwaa.Value;
            byte flashBlue = (byte)trbBlueParIwaa.Value;

            // Disable trackbars during flashing
            trbRedParIwaa.Enabled = false;
            trbGreenParIwaa.Enabled = false;
            trbBlueParIwaa.Enabled = false;

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

            // Only restore original values if lights are supposed to be ON
            if (lightsTogglePower)
            {
                dmxData[0] = flashRed;
                dmxData[1] = flashGreen;
                dmxData[2] = flashBlue;
            }
            else
            {
                // Keep lights off
                dmxData[0] = 0;
                dmxData[1] = 0;
                dmxData[2] = 0;
            }

            // Re-enable trackbars
            trbRedParIwaa.Enabled = true;
            trbGreenParIwaa.Enabled = true;
            trbBlueParIwaa.Enabled = true;

            // Reset button
            btnFlashLightsIwaa.Text = "Make the lights flash";
            btnFlashLightsIwaa.Click += btnFlashLightsIwaa_Click;
        }

        private void trbFlashIntervalIwaa_Scroll(object sender, EventArgs e)
        {
            //lblFlashIntervalIwaa.Text = trbFlashIntervalIwaa.Value.ToString();
        }

        //color button
        #region

        private void btnColorRedIwaa_Click(object sender, EventArgs e)
        {
            turnRed();
        }

        private void btnColorGreenIwaa_Click(object sender, EventArgs e)
        {
            turnGreen();
        }
        private void btnColorBlueIwaa_Click(object sender, EventArgs e)
        {
            turnBlue();
        }
        private void btnColorWhiteIwaa_Click(object sender, EventArgs e)
        {
            turnWhite();
        }
        private void btnColorPurpleIwaa_Click(object sender, EventArgs e)
        {
            turnPurple();
        }
        private void btnColorOrangeIwaa_Click(object sender, EventArgs e)
        {
            turnOrange();
        }
        private void btnColorYellowIwaa_Click(object sender, EventArgs e)
        {
            turnYellow();
        }

        private void disableButtons()
        {
            btnColorRedIwaa.Enabled = false;
            btnColorGreenIwaa.Enabled = false;
            btnColorBlueIwaa.Enabled = false;
            btnColorWhiteIwaa.Enabled = false;
            btnColorPurpleIwaa.Enabled = false;
            btnColorOrangeIwaa.Enabled = false;
            btnColorYellowIwaa.Enabled = false;
        }

        private void enableButtons()
        {
            btnColorRedIwaa.Enabled = true;
            btnColorGreenIwaa.Enabled = true;
            btnColorBlueIwaa.Enabled = true;
            btnColorWhiteIwaa.Enabled = true;
            btnColorPurpleIwaa.Enabled = true;
            btnColorOrangeIwaa.Enabled = true;
            btnColorYellowIwaa.Enabled = true;
        }

        #endregion

        //show functions
        #region

        private void turnRed()
        {
            dmxData[0] = 255;  // Red
            dmxData[1] = 0;    // Green
            dmxData[2] = 0;    // Blue

            savedDmxData[0] = 255;
            savedDmxData[1] = 0;
            savedDmxData[2] = 0;

            trbRedParIwaa.Value = 255;
            trbGreenParIwaa.Value = 0;
            trbBlueParIwaa.Value = 0;

            trbMHredIwaa.Value = 255;
            trbMHgreenIwaa.Value = 0;
            trbMHblueIwaa.Value = 0;
            trbMHwhiteIwaa.Value = 0;

            red = trbMHredIwaa.Value;
            green = trbMHgreenIwaa.Value;
            blue = trbMHblueIwaa.Value;
            white = trbMHwhiteIwaa.Value;
        }

        private void turnGreen()
        {
            dmxData[0] = 0;  // Red
            dmxData[1] = 255;    // Green
            dmxData[2] = 0;    // Blue

            savedDmxData[0] = 0;
            savedDmxData[1] = 255;
            savedDmxData[2] = 0;

            trbRedParIwaa.Value = 0;
            trbGreenParIwaa.Value = 255;
            trbBlueParIwaa.Value = 0;

            trbMHredIwaa.Value = 0;
            trbMHgreenIwaa.Value = 255;
            trbMHblueIwaa.Value = 0;
            trbMHwhiteIwaa.Value = 0;

            red = trbMHredIwaa.Value;
            green = trbMHgreenIwaa.Value;
            blue = trbMHblueIwaa.Value;
            white = trbMHwhiteIwaa.Value;
        }

        private void turnBlue()
        {
            dmxData[0] = 0;  // Red
            dmxData[1] = 0;    // Green
            dmxData[2] = 255;    // Blue

            savedDmxData[0] = 0;
            savedDmxData[1] = 0;
            savedDmxData[2] = 255;

            trbRedParIwaa.Value = 0;
            trbGreenParIwaa.Value = 0;
            trbBlueParIwaa.Value = 255;

            trbMHredIwaa.Value = 0;
            trbMHgreenIwaa.Value = 0;
            trbMHblueIwaa.Value = 255;
            trbMHwhiteIwaa.Value = 0;

            red = trbMHredIwaa.Value;
            green = trbMHgreenIwaa.Value;
            blue = trbMHblueIwaa.Value;
            white = trbMHwhiteIwaa.Value;
        }

        private void turnWhite()
        {
            dmxData[0] = 255;  // Red
            dmxData[1] = 255;    // Green
            dmxData[2] = 255;    // Blue

            savedDmxData[0] = 255;
            savedDmxData[1] = 255;
            savedDmxData[2] = 255;

            trbRedParIwaa.Value = 255;
            trbGreenParIwaa.Value = 255;
            trbBlueParIwaa.Value = 255;

            trbMHredIwaa.Value = 0;
            trbMHgreenIwaa.Value = 0;
            trbMHblueIwaa.Value = 0;
            trbMHwhiteIwaa.Value = 255;

            red = trbMHredIwaa.Value;
            green = trbMHgreenIwaa.Value;
            blue = trbMHblueIwaa.Value;
            white = trbMHwhiteIwaa.Value;
        }

        private void turnPurple()
        {
            dmxData[0] = 255;  // Red
            dmxData[1] = 0;    // Green
            dmxData[2] = 255;    // Blue

            savedDmxData[0] = 255;
            savedDmxData[1] = 0;
            savedDmxData[2] = 255;

            trbRedParIwaa.Value = 255;
            trbGreenParIwaa.Value = 0;
            trbBlueParIwaa.Value = 255;

            trbMHredIwaa.Value = 255;
            trbMHgreenIwaa.Value = 0;
            trbMHblueIwaa.Value = 255;
            trbMHwhiteIwaa.Value = 0;

            red = trbMHredIwaa.Value;
            green = trbMHgreenIwaa.Value;
            blue = trbMHblueIwaa.Value;
            white = trbMHwhiteIwaa.Value;
        }

        private void turnOrange()
        {
            dmxData[0] = 204;  // Red
            dmxData[1] = 85;    // Green
            dmxData[2] = 0;    // Blue

            savedDmxData[0] = 204;
            savedDmxData[1] = 85;
            savedDmxData[2] = 0;

            trbRedParIwaa.Value = 204;
            trbGreenParIwaa.Value = 85;
            trbBlueParIwaa.Value = 0;

            trbMHredIwaa.Value = 204;
            trbMHgreenIwaa.Value = 85;
            trbMHblueIwaa.Value = 0;
            trbMHwhiteIwaa.Value = 0;

            red = trbMHredIwaa.Value;
            green = trbMHgreenIwaa.Value;
            blue = trbMHblueIwaa.Value;
            white = trbMHwhiteIwaa.Value;
        }

        private void turnYellow()
        {
            dmxData[0] = 255;  // Red
            dmxData[1] = 255;    // Green
            dmxData[2] = 0;    // Blue

            savedDmxData[0] = 255;
            savedDmxData[1] = 255;
            savedDmxData[2] = 0;

            trbRedParIwaa.Value = 255;
            trbGreenParIwaa.Value = 255;
            trbBlueParIwaa.Value = 0;

            trbMHredIwaa.Value = 255;
            trbMHgreenIwaa.Value = 255;
            trbMHblueIwaa.Value = 0;
            trbMHwhiteIwaa.Value = 0;

            red = trbMHredIwaa.Value;
            green = trbMHgreenIwaa.Value;
            blue = trbMHblueIwaa.Value;
            white = trbMHwhiteIwaa.Value;
        }

        private void turnOff()
        {
            disableButtons();
            // Turn off flashing first if it's active
            if (toggleFlashing)
            {
                toggleFlashing = false;
                btnFlashLightsIwaa.Text = "Make the lights flash";
                trbFlashIntervalIwaa.Enabled = false;
            }

            dmxData[0] = 0;
            dmxData[1] = 0;
            dmxData[2] = 0;

            trbRedParIwaa.Enabled = false;
            trbGreenParIwaa.Enabled = false;
            trbBlueParIwaa.Enabled = false;
            lightsTogglePower = false;
        }

        private void turnOn()
        {
            enableButtons();
            dmxData[0] = (byte)savedDmxData[0];
            dmxData[1] = (byte)savedDmxData[1];
            dmxData[2] = (byte)savedDmxData[2];

            trbRedParIwaa.Enabled = true;
            trbGreenParIwaa.Enabled = true;
            trbBlueParIwaa.Enabled = true;
            lightsTogglePower = true;

            SendDMXFrame();
        }

        private void startFlashing()
        {
            toggleFlashing = true;
            btnFlashLightsIwaa.Text = "Stop Flashing";
            trbFlashIntervalIwaa.Enabled = true;
            strobe = 255;
            LightsFlashing();
        }

        private void stopFlashing()
        {
            toggleFlashing = false;
            btnFlashLightsIwaa.Text = "Make the lights flash";
            trbFlashIntervalIwaa.Enabled = false;
            strobe = 0;
            enableButtons();
        }

        private void pan(int angle)
        {
            pan1 = pan2 = pan3 = pan4 = angle;
        }

        private void tilt(int angle)
        {
            tilt1 = tilt2 = tilt3 = tilt4 = angle;
        }

        #endregion

        private void LoadComboBox()
        {
            string query = "SELECT Id, name FROM savedShows"; // columns you want

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                comboBox1.DataSource = dt;
                comboBox1.DisplayMember = "Name"; // column shown to user
                comboBox1.ValueMember = "Id";     // underlying value
            }
        }

        private async void btnPlayIwaa_Click(object sender, EventArgs e)
        {
            dim1 = dim2 = dim3 = dim4 = 255;

            if (isPlayingShow)
            {
                // Stop the currently playing show
                StopShow();
                return;
            }

            if (comboBox1.SelectedValue == null)
            {
                MessageBox.Show("Please select a show to play.");
                return;
            }

            int showId = (int)comboBox1.SelectedValue;
            await PlayShow(showId);
        }

        private async Task PlayShow(int showId)
        {
            string connectionString = @"Data Source=localhost\sqlexpress;Initial Catalog=FunDatabasename;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
            string query = "SELECT functionSequence FROM savedShows WHERE Id = @Id";

            string functionSequence = "";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", showId);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        functionSequence = result.ToString();
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(functionSequence))
            {
                MessageBox.Show("No function sequence found for this show.");
                return;
            }

            // Parse the function sequence
            string[] functions = functionSequence.Split(new[] { ", ", "," }, StringSplitOptions.RemoveEmptyEntries);

            // Start playing
            isPlayingShow = true;
            playShowCancellationToken = new CancellationTokenSource();
            btnPlayIwaa.Text = "STOP";

            try
            {
                foreach (string functionWithDuration in functions)
                {
                    if (playShowCancellationToken.Token.IsCancellationRequested)
                        break;

                    string functionName = "";
                    int duration = 2; // Default duration in seconds

                    string trimmedFunction = functionWithDuration.Trim();

                    // Check if it's pan or tilt with angle (format: pan:90:3 or tilt:45:2)
                    if (trimmedFunction.Contains(":"))
                    {
                        string[] parts = trimmedFunction.Split(':');
                        if (parts.Length >= 3)
                        {
                            functionName = $"{parts[0]}:{parts[1]}"; // e.g., "pan:90"
                            if (int.TryParse(parts[2], out int parsedDuration))
                            {
                                duration = parsedDuration;
                            }
                        }
                    }
                    else
                    {
                        // Original format: functionName + duration (e.g., "turnRed5")
                        int lastDigitIndex = trimmedFunction.Length - 1;

                        while (lastDigitIndex >= 0 && char.IsDigit(trimmedFunction[lastDigitIndex]))
                        {
                            lastDigitIndex--;
                        }

                        if (lastDigitIndex < trimmedFunction.Length - 1)
                        {
                            functionName = trimmedFunction.Substring(0, lastDigitIndex + 1);
                            string durationString = trimmedFunction.Substring(lastDigitIndex + 1);

                            if (int.TryParse(durationString, out int parsedDuration))
                            {
                                duration = parsedDuration;
                            }
                        }
                        else
                        {
                            functionName = trimmedFunction;
                        }
                    }

                    // Execute the function
                    await ExecuteFunction(functionName);

                    // Wait for the specified duration
                    if (!functionName.StartsWith("startFlashing", StringComparison.OrdinalIgnoreCase))
                    {
                        await Task.Delay(duration * 1000, playShowCancellationToken.Token);
                    }
                    else
                    {
                        await Task.Delay(duration * 1000, playShowCancellationToken.Token);
                        stopFlashing();
                        await Task.Delay(500, playShowCancellationToken.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Show was stopped
            }
            finally
            {
                StopShow();
            }
        }

        private async Task ExecuteFunction(string functionName)
        {
            // Check if this is a pan or tilt function with angle parameter
            if (functionName.Contains(":"))
            {
                string[] parts = functionName.Split(':');
                string funcName = parts[0].ToLower();

                if (funcName == "pan" && parts.Length >= 2)
                {
                    if (int.TryParse(parts[1], out int angle))
                    {
                        pan(angle);
                        MoveIt();
                    }
                }
                else if (funcName == "tilt" && parts.Length >= 2)
                {
                    if (int.TryParse(parts[1], out int angle))
                    {
                        tilt(angle);
                        MoveIt();
                    }
                }
                return;
            }

            // Original switch for other functions
            switch (functionName.ToLower())
            {
                case "turnred":
                    turnRed();
                    break;
                case "turngreen":
                    turnGreen();
                    break;
                case "turnblue":
                    turnBlue();
                    break;
                case "turnwhite":
                    turnWhite();
                    break;
                case "turnpurple":
                    turnPurple();
                    break;
                case "turnorange":
                    turnOrange();
                    break;
                case "turnyellow":
                    turnYellow();
                    break;
                case "turnon":
                    turnOn();
                    break;
                case "turnoff":
                    turnOff();
                    break;
                case "startflashing":
                    startFlashing();
                    break;
                default:
                    MessageBox.Show($"Unknown function: {functionName}");
                    break;
            }

            await Task.Delay(50);
        }

        //stop the show
        private void StopShow()
        {
            isPlayingShow = false;
            playShowCancellationToken?.Cancel();
            btnPlayIwaa.Text = "PLAY";
            tilt1 = tilt2 = tilt3 = tilt4 = pan1 = pan2 = pan3 = pan4 = 0;

            // Stop flashing if it's active
            if (toggleFlashing)
            {
                stopFlashing();
            }
        }

        private void MoveIt()
        {
            // Open the COM port (replace COM3 with your actual port)
            driver.DmxToDefault("COM12");

            // MASTER dimmer full
            driver.DmxLoadBuffer(21, 255, 512);

            // Strobe off
            driver.DmxLoadBuffer(22, (byte)strobe, 512);

            // RGBW full
            driver.DmxLoadBuffer(23, (byte)red, 512); // Red
            driver.DmxLoadBuffer(24, (byte)green, 512); // Green
            driver.DmxLoadBuffer(25, (byte)blue, 512); // Blue
            driver.DmxLoadBuffer(26, (byte)white, 512); // White

            // Head 1 – point tilt
            driver.DmxLoadBuffer(27, (byte)pan1, 512);  // Pan center
            driver.DmxLoadBuffer(28, (byte)tilt1, 512);    // tilt tilt
            driver.DmxLoadBuffer(29, (byte)dim1, 512);  // Dimmer full

            // Head 2 – point tilt
            driver.DmxLoadBuffer(30, (byte)pan2, 512);
            driver.DmxLoadBuffer(31, (byte)tilt2, 512);
            driver.DmxLoadBuffer(32, (byte)dim2, 512);

            // Head 3 – point tilt
            driver.DmxLoadBuffer(33, (byte)pan3, 512);
            driver.DmxLoadBuffer(34, (byte)tilt3, 512);
            driver.DmxLoadBuffer(35, (byte)dim3, 512);

            // Head 4 – point tilt
            driver.DmxLoadBuffer(36, (byte)pan4, 512);
            driver.DmxLoadBuffer(37, (byte)tilt4, 512);
            driver.DmxLoadBuffer(38, (byte)dim4, 512);

            // Speed / Strobe (slow)
            driver.DmxLoadBuffer(39, (byte)speed, 512);

            // Send all 19 channels to the lights
            driver.DmxSendCommand(19);

        }
        


        private void trbMHredIwaa_Scroll(object sender, EventArgs e)
        {
            red = trbMHredIwaa.Value;
        }

        private void trbMHgreenIwaa_Scroll(object sender, EventArgs e)
        {
            green = trbMHgreenIwaa.Value;
        }

        private void trbMHblueIwaa_Scroll(object sender, EventArgs e)
        {
            blue = trbMHblueIwaa.Value;
        }

        private void trbMHwhiteIwaa_Scroll(object sender, EventArgs e)
        {
            white = trbMHwhiteIwaa.Value;
        }

        private void trbPanAllIwaa_Scroll(object sender, EventArgs e)
        {
            pan1 = trbPanAllIwaa.Value;
            pan2 = trbPanAllIwaa.Value;
            pan3 = trbPanAllIwaa.Value;
            pan4 = trbPanAllIwaa.Value;
        }

        private void trbPan1Iwaa_Scroll(object sender, EventArgs e)
        {
            pan1 = trbPan1Iwaa.Value;
        }

        private void trbPan2Iwaa_Scroll(object sender, EventArgs e)
        {
            pan2 = trbPan2Iwaa.Value;
        }

        private void trbPan3Iwaa_Scroll(object sender, EventArgs e)
        {
            pan3 = trbPan3Iwaa.Value;
        }

        private void trbPan4Iwaa_Scroll(object sender, EventArgs e)
        {
            pan4 = trbPan4Iwaa.Value;
        }

        private void trbTiltAllIwaa_Scroll(object sender, EventArgs e)
        {
            tilt4 = trbTiltAllIwaa.Value;
            tilt3 = trbTiltAllIwaa.Value;
            tilt2 = trbTiltAllIwaa.Value;
            tilt1 = trbTiltAllIwaa.Value;
        }

        private void trbTilt1Iwaa_Scroll(object sender, EventArgs e)
        {
            tilt1 = trbTilt1Iwaa.Value;
        }

        private void trbTilt2Iwaa_Scroll(object sender, EventArgs e)
        {
            tilt2 = trbTilt2Iwaa.Value;
        }

        private void trbTilt3Iwaa_Scroll(object sender, EventArgs e)
        {
            tilt3 = trbTilt3Iwaa.Value;
        }

        private void trbTilt4Iwaa_Scroll(object sender, EventArgs e)
        {
            tilt4 = trbTilt4Iwaa.Value;
        }

        private void btnCollapsePanIwaa_Click(object sender, EventArgs e)
        {
            if (GrbPan.Visible == true)
            {
                GrbPan.Visible = false;
            }
            else
            {
                GrbPan.Visible = true;
            }
        }

        private void btnCollapseTiltIwaa_Click(object sender, EventArgs e)
        {
            if (GrbTilt.Visible == true)
            {
                GrbTilt.Visible = false;
            }
            else
            {
                GrbTilt.Visible = true;
            }
        }              

        private void btnStrobeIwaa_Click(object sender, EventArgs e)
        {
            if (strobe == 0)
            {
                strobe = 255;
            }
            else
            {
                strobe = 0;
            }
            MoveIt();
        }

        private void trbStrobeIntervalIWaa_Scroll(object sender, EventArgs e)
        {
            strobe = trbStrobeIntervalIWaa.Value;
        }

        private void trbMovementSpeedIwaa_Scroll(object sender, EventArgs e)
        {
            speed = trbMovementSpeedIwaa.Value;
        }

        private void btnResetIwaa_Click(object sender, EventArgs e)
        {
            tilt1 = tilt2 = tilt3 = tilt4 = pan1 = pan2 = pan3 = pan4 = red = blue = green = white = dim1 = dim2 = dim3 = dim4 = strobe = 0;

            // Reset toggle switches
            cbxSwitc1Iwaa.Checked = false;
            cbxSwitch2Iwaa.Checked = false;
            cbxSwitch3Iwaa.Checked = false;
            cbxSwitch4Iwaa.Checked = false;

            trbPan1Iwaa.Value = 0;
            trbPan2Iwaa.Value = 0;
            trbPan3Iwaa.Value = 0;
            trbPan4Iwaa.Value = 0;
            trbPanAllIwaa.Value = 0;

            trbTilt1Iwaa.Value = 0;
            trbTilt2Iwaa.Value = 0;
            trbTilt3Iwaa.Value = 0;
            trbTilt4Iwaa.Value = 0;
            trbTiltAllIwaa.Value = 0;

            trbMHredIwaa.Value = 0;
            trbMHgreenIwaa.Value = 0;
            trbMHblueIwaa.Value = 0;
            trbMHwhiteIwaa.Value = 0;

            trbMovementSpeedIwaa.Value = 0;
            trbStrobeIntervalIWaa.Value = 0;

        }

        private void Numericbox_keypress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            if (!char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void Numericbox_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;

            if (int.TryParse(tb.Text, out int value))
            {
                if (value > 170)
                    tb.Text = "170";

                tb.SelectionStart = tb.Text.Length;
            }
            else
            {
                tb.Text = "";
            }
        }

        private void AttachHandlers(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is TextBox tb)
                {
                    tb.KeyPress += Numericbox_keypress;
                    tb.TextChanged += Numericbox_TextChanged;
                }

                // Recursively check nested controls
                if (c.HasChildren)
                    AttachHandlers(c);
            }
        }

        private void cbxSwitc1Iwaa_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxSwitc1Iwaa.Checked)
            {
                dim1 = 255;
            } else
            {
                dim1 = 0;
            }
        }

        private void cbxSwitch2Iwaa_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxSwitch2Iwaa.Checked)
            {
                dim2 = 255;
            }
            else
            {
                dim2 = 0;
            }
        }

        private void cbxSwitch3Iwaa_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxSwitch3Iwaa.Checked)
            {
                dim3 = 255;
            }
            else
            {
                dim3 = 0;
            }
        }

        private void cbxSwitch4Iwaa_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxSwitch4Iwaa.Checked)
            {
                dim4 = 255;
            }
            else
            {
                dim4 = 0;
            }
        }

        private void BtnHome1Iwaa_Click(object sender, EventArgs e)
        {
            tbcPagesIwaa.SelectedIndex = 0;
        }

        private void btnHome2Iwaa_Click(object sender, EventArgs e)
        {
            tbcPagesIwaa.SelectedIndex = 0;
        }

        private void btnHome3Iwaa_Click(object sender, EventArgs e)
        {
            tbcPagesIwaa.SelectedIndex = 0;
        }
    }
}
