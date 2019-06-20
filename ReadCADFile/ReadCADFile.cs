using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
//
//
namespace ReadCADFile
{
    
    public partial class ReadCADFile : Form
    {
        public ShowDwgFile showdwgfile = new ShowDwgFile();
       
        public ReadCADFile()
        {
            InitializeComponent();
            //this.DWGPictureBox.Hide();
        }
       
        #region operation
        protected virtual void OnResize( object sender,EventArgs e)
        {
            base.OnResize(e);

            showdwgfile.ImageControl_OnResize(sender,e);

        }

        protected virtual void  OnPaint(object sender, PaintEventArgs e)
        {

            showdwgfile.ImageControl_OnPaint(sender, e);

        }

        protected virtual void OnMouseDown(object sender, MouseEventArgs e)
        {
            base.OnMouseDown(e);
            showdwgfile.ImageControl_OnMouseDown(sender, e);
        }
         protected virtual void OnMouseUp(object sender, MouseEventArgs e)
        {
            base.OnMouseUp(e);
            showdwgfile.ImageControl_OnMouseUp(sender, e);

        }

         protected virtual void OnMouseMove(object sender, MouseEventArgs e)
        {
            base.OnMouseMove(e);
            showdwgfile.ImageControl_OnMouseMove(sender, e);
        }

         protected virtual void OnMouseWheel(object sender, MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            showdwgfile.ImageControl_OnMouseWheel(sender, e);
        }

         protected  override void OnMouseWheel( MouseEventArgs e)
         {
             base.OnMouseWheel(e);

             showdwgfile.ImageControl_OnMouseWheel(null, e);
         }
        #endregion operation
      
        #region menu
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            //打开文件
            this.pictureBox1.Hide();
           // showdwgfile.ImageControl = this.DWGPictureBox;
            this.DWGPictureBox.Show();
            showdwgfile.ImageControl = DWGPictureBox;
            showdwgfile.openFile(null);
            //savefile();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            //查看缩略图
            //this.Controls.Clear();

            pictureBox1.Image = showdwgfile.GetDwgImage(null);
            pictureBox1.Show();
        }
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            //保存图像
            showdwgfile.SaveToImageFile();
        }

        #endregion menu


        #region 拖拽功能
        private void DWGPictureBox_DragOver(object sender, DragEventArgs e)
            {
                if ((e.AllowedEffect & DragDropEffects.Link) == DragDropEffects.Link)
                {
                    e.Effect = DragDropEffects.Link;
                }
            }

        private void DWGPictureBox_DragEnter(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void DWGPictureBox_DragDrop(object sender, DragEventArgs e)
        {
            
            //以下拖曳添加文件
            string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            if (path.IndexOf(".dwg", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf(".dxf", StringComparison.OrdinalIgnoreCase) >= 0 
                // if (path.Contains(".jpg")|| path.Contains(".bmp") || path.Contains(".png") ||path.Contains(".gif") 
                )
            {
                this.DWGPictureBox.Show();
                showdwgfile.ImageControl = DWGPictureBox;
                showdwgfile.openFile(path);

            }
            else
            {
                MessageBox.Show("文件类型不正确");
            }
        }

        #endregion 拖拽功能
      


    }
}
