using System;
using System.Drawing;
using System.Windows.Forms;

namespace study1
{
    public class Form1 : Form
    {
        //Bitmap
        private Bitmap _myBitmap;
        private Bitmap _my2ndBitmap;
        //Main form items
        private MainMenu _myMainMenu;
        private MenuItem _menuItem1;
        private MenuItem _menuItem2;
        //In menuItem1:
        private MenuItem _fileLoad;
        private MenuItem _fileExit;
        //In menuItem2:
        private readonly System.ComponentModel.Container _components = null;
        private MenuItem _horizontalConc;
        private MenuItem _verticalConc;
        private int _direction;
        private Form1()
        {
            InitializeComponent();
            _myBitmap = new Bitmap(2, 2);
            _my2ndBitmap = new Bitmap(2, 2);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // Inits the main menu, it's tabs, then their child tabs
            this._myMainMenu = new MainMenu();
            this._menuItem1 = new MenuItem();
            this._menuItem2 = new MenuItem();
            
            this._fileLoad = new MenuItem();
            this._fileExit = new MenuItem();
            
            this._horizontalConc = new MenuItem();
            this._verticalConc = new MenuItem();
            
            //Main Menu Tabs
            this._myMainMenu.MenuItems.AddRange(new[]
            {
                this._menuItem1,
                this._menuItem2,
            });

            //First Dropdown Menu Tab
            //Name
            this._menuItem1.Text = "File";
            //Index
            this._menuItem1.Index = 0;
            //Child nodes
            this._menuItem1.MenuItems.AddRange(new []
            {
            this._fileLoad,
            this._fileExit,
                });

            //File Load
            this._fileLoad.Index = 0;
            this._fileLoad.Shortcut = Shortcut.CtrlL;
            this._fileLoad.Text = "Load";
            this._fileLoad.Click += this.File_Load;

            //File Exit
            this._fileExit.Index = 1;
            this._fileExit.Text = "Exit";
            this._fileExit.Click += this.File_Exit;

            //Third Dropdown Menu Tab
            //Name
            this._menuItem2.Text = "Concatenate";
            //Index
            this._menuItem2.Index = 1;
            //Child nodes
            this._menuItem2.MenuItems.AddRange(new []
            {
            this._horizontalConc,
            this._verticalConc,
                });

            this._horizontalConc.Index = 0;
            this._horizontalConc.Text = "Horizontally";
            this._horizontalConc.Click += this._applyHorizontalConc;
            
            this._verticalConc.Index = 1;
            this._verticalConc.Text = "Vertically";
            this._verticalConc.Click += this._applyVerticalConc;
            
            
            //Form1
            this.AutoScaleBaseSize = new Size(5, 13);
            this.BackColor = SystemColors.ControlLight;
            this.ClientSize = new Size(350, 350);
            this.Menu = this._myMainMenu;
            this.Name = $"Study into .NET GUI & Image Processing";
            this.Text = "Concatenate Images";
            _direction = 0;
            //ActiveControl = null;
            //this.Focus();
            
            //this.Load += this.Form1_Load;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        // TODO: Fix this
        private void _applyHorizontalConc(object sender, EventArgs e)
        {
            _direction = 1;
            this.Invalidate();
        }
        
        private void _applyVerticalConc(object sender, EventArgs e)
        {
            _direction = 2;
            this.Invalidate();
        }

        //private void Form1_Load(object sender, EventArgs e) { }
        
        //File Load Functions
        private void File_Load(object sender, EventArgs e)
        {
            _direction = 0;
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "Bitmap files (*.bmp)|*.bmp|Jpeg files (*.jpg)|*.jpg|All valid files (*.bmp/*.jpg)|*.bmp;*.jpg";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = false;

            if (DialogResult.OK != openFileDialog.ShowDialog()) return;
            
            _myBitmap = (Bitmap)Image.FromFile(openFileDialog.FileNames[0], false);
            _my2ndBitmap = (Bitmap)Image.FromFile(openFileDialog.FileNames[1], false);
            BitmapOperations.Convert2GrayScaleFast(_myBitmap);
            BitmapOperations.Convert2GrayScaleFast(_my2ndBitmap);
            this.ClientSize = new Size(((_myBitmap.Width + _my2ndBitmap.Width) / 3) + 5, ((_myBitmap.Height +
                _my2ndBitmap.Height) / 3) + 5);
            MessageBox.Show("Images loaded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        //File_Exit Function
        private void File_Exit(object sender, EventArgs e)
        {
            this.Close();
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics gg = e.Graphics;
            if (_direction == 1)
            {
               gg.DrawImage(_myBitmap, new Rectangle
                           (this.AutoScrollPosition.X, this.AutoScrollPosition.Y, 
                               (_myBitmap.Width / 3), (_myBitmap.Height / 3)
                           ));
               gg.DrawImage(_my2ndBitmap,new Rectangle
                           (this.AutoScrollPosition.X + (_myBitmap.Width / 3), this.AutoScrollPosition.Y, 
                               (_my2ndBitmap.Width / 3), (_my2ndBitmap.Height / 3)
                           )); 
            }
            else if (_direction == 2)
            {
                gg.DrawImage(_myBitmap, new Rectangle
                (this.AutoScrollPosition.X, this.AutoScrollPosition.Y, 
                    (_myBitmap.Width / 3), (_myBitmap.Height / 3)
                ));
                gg.DrawImage(_my2ndBitmap,new Rectangle
                (this.AutoScrollPosition.X, this.AutoScrollPosition.Y + (_myBitmap.Height / 3), 
                    (_my2ndBitmap.Width / 3), (_my2ndBitmap.Height / 3)
                )); 
            }
        }
        [STAThread]
        // Program Entry Point
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
