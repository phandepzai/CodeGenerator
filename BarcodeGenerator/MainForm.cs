using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace BarcodeGenerator
{
    public class MainForm : Form
    {
        // Các thành phần giao diện - Khai báo các điều khiển sẽ dùng trong form
        private TextBox txtInput; // Ô nhập liệu cho dữ liệu mã
        private PictureBox picBarcode1; // Hộp hiển thị mã QR/DataMatrix thứ nhất
        private PictureBox picBarcode2; // Hộp hiển thị mã QR thứ hai (chỉ dùng cho QR Code)
        private Button btnSave; // Nút để lưu ảnh mã
        private Button btnReset; // Nút để reset giao diện
        private RadioButton rbQR; // Nút chọn để tạo mã QR
        private RadioButton rbDataMatrix; // Nút chọn để tạo mã DataMatrix
        private Label lblSavePath; // Nhãn hiển thị đường dẫn lưu ảnh
        private Label lblAuthor; // Nhãn hiển thị tên tác giả
        private Label lblBarcode1; // Nhãn hiển thị dữ liệu của mã thứ nhất
        private Label lblBarcode2; // Nhãn hiển thị dữ liệu của mã thứ hai
        private Bitmap generatedBarcode1; // Hình ảnh mã thứ nhất được tạo
        private Bitmap generatedBarcode2; // Hình ảnh mã thứ hai được tạo

        // Hằng số để đặt placeholder cho TextBox - Dùng để hiển thị gợi ý trong ô nhập liệu
        private const int EM_SETCUEBANNER = 0x1501;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        // Hàm khởi tạo form
        public MainForm()
        {
            InitializeComponents(); // Khởi tạo các thành phần giao diện
            SetupEvents(); // Thiết lập sự kiện cho các nút
            BarcodeTypeChanged(null, EventArgs.Empty); // Cập nhật giao diện ban đầu dựa trên loại mã
        }

        // Khởi tạo các thành phần giao diện
        private void InitializeComponents()
        {
            this.Text = "Tạo mã"; // Tiêu đề của form
            this.ClientSize = new Size(500, 420); // Kích thước form: 500x510 pixel
            this.MinimumSize = new Size(500, 450); // Kích thước tối thiểu của form
            this.Font = new Font("Arial", 12, FontStyle.Regular); // Font chữ mặc định cho form
            this.StartPosition = FormStartPosition.CenterScreen; // Hiển thị form ở giữa màn hình
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // Không cho thay đổi kích thước form
            this.MaximizeBox = false; // Ẩn nút phóng to
            this.Icon = new Icon(typeof(MainForm), "icon.ico");


            // Ô nhập liệu
            txtInput = new TextBox
            {
                Size = new Size(300, 50),
                Multiline = false,
                Font = new Font("Arial", 14),
                TextAlign = HorizontalAlignment.Center
            };
            txtInput.Location = new Point((this.ClientSize.Width - txtInput.Width) / 2, 20);
            SetPlaceholderText(txtInput, "Nhập dữ liệu mã tại đây"); // Thêm văn bản gợi ý

            // Nút chọn QR Code
            rbQR = new RadioButton
            {
                Text = "QR Code", // Tên hiển thị
                Font = new Font("Arial", 10, FontStyle.Bold), // Font chữ đậm
                AutoSize = true, // Tự động điều chỉnh kích thước
                Checked = true // Mặc định chọn QR Code
            };

            // Nút chọn DataMatrix
            rbDataMatrix = new RadioButton
            {
                Text = "DataMatrix", // Tên hiển thị
                Font = new Font("Arial", 10, FontStyle.Bold), // Font chữ đậm
                AutoSize = true // Tự động điều chỉnh kích thước
            };

            int radioSpacing = 20; // Khoảng cách giữa hai nút chọn
            int totalRadioWidth = rbQR.Width + rbDataMatrix.Width + radioSpacing; // Tổng chiều rộng của hai nút
            rbQR.Location = new Point((this.ClientSize.Width - totalRadioWidth) / 2, 70); // Đặt nút QR ở giữa
            rbDataMatrix.Location = new Point(rbQR.Right + radioSpacing, 70); // Đặt nút DataMatrix bên phải nút QR

            // Nút "Lưu ảnh"
            btnSave = new Button
            {
                Text = "LƯU ẢNH", // Tên hiển thị
                Size = new Size(100, 40), // Kích thước nút
                Font = new Font("Segoe UI", 10, FontStyle.Regular), // Font chữ
                BackColor = Color.LightGreen // Màu nền
            };

            // Nút "Reset"
            btnReset = new Button
            {
                Text = "RESET", // Tên hiển thị
                Size = new Size(100, 40), // Kích thước nút
                Font = new Font("Segoe UI", 10, FontStyle.Regular), // Font chữ
                BackColor = Color.Moccasin // Màu nền
            };

            int buttonSpacing = 10; // Khoảng cách giữa các nút
            int totalButtonWidth = btnSave.Width + btnReset.Width + buttonSpacing; // Tổng chiều rộng các nút
            btnSave.Location = new Point((this.ClientSize.Width - totalButtonWidth) / 2, 100); // Đặt nút "Lưu ảnh" ở giữa
            btnReset.Location = new Point(btnSave.Right + buttonSpacing, 100); // Đặt nút "Reset" bên phải

            // Hộp hiển thị mã thứ nhất
            picBarcode1 = new PictureBox
            {
                BorderStyle = BorderStyle.None,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(150, 150)
            };

            // Hộp hiển thị mã thứ hai
            picBarcode2 = new PictureBox
            {
                BorderStyle = BorderStyle.None,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(150, 150),
                Visible = false
            };

            // Nhãn hiển thị dữ liệu mã thứ nhất
            lblBarcode1 = new Label
            {
                Text = "",
                Font = new Font("Arial", 12, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(150, 60), // Tăng chiều cao lên 60
                MaximumSize = new Size(150, 0), // Chiều rộng cố định 150, chiều cao không giới hạn
                TextAlign = ContentAlignment.TopCenter,
                BackColor = Color.Transparent,
            };

            // Nhãn hiển thị dữ liệu mã thứ hai
            lblBarcode2 = new Label
            {
                Text = "",
                Font = new Font("Arial", 12, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(150, 60), // Tăng chiều cao lên 60
                MaximumSize = new Size(150, 0), // Chiều rộng cố định 150, chiều cao không giới hạn
                TextAlign = ContentAlignment.TopCenter,
                BackColor = Color.Transparent,
                Visible = false
            };

            // Nhãn hiển thị đường dẫn lưu
            lblSavePath = new Label
            {
                Text = string.Empty,
                Font = new Font("Arial", 8),
                ForeColor = Color.Green,
                AutoSize = false,
                Size = new Size(350, 60),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Nhãn hiển thị tên tác giả
            lblAuthor = new Label
            {
                Text = "Nông Văn Phấn",
                Font = new Font("Arial", 8),
                ForeColor = Color.Silver,
                AutoSize = true
            };

            // Thêm tất cả điều khiển vào form
            this.Controls.AddRange(new Control[] {
                txtInput, rbQR, rbDataMatrix,
                btnSave, btnReset,
                picBarcode1, picBarcode2,
                lblBarcode1, lblBarcode2,
                lblSavePath, lblAuthor
            });

            UpdateBarcodeLayout();
            this.Resize += (s, e) => UpdateBarcodeLayout();
        }
        private void PicBarcode_Paint(object sender, PaintEventArgs e)
        {
            if (sender is PictureBox pb)
            {
                // Màu xám cho viền  
                Color borderColor = Color.Silver; // Hoặc Color.Gray, Color.DarkGray tùy ý  
                int borderWidth = 1; // Độ dày của viền  

                // Vẽ hình chữ nhật làm viền  
                using (Pen pen = new Pen(borderColor, borderWidth))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pb.Width - 1, pb.Height - 1);
                }
            }
        }

        // Cập nhật vị trí các thành phần giao diện
        private void UpdateBarcodeLayout()
        {
            int barcodeWidth = 150;
            int barcodeHeight = 150;
            int labelHeight = 60; // Xóa hoặc bỏ qua dòng này
            int startY = 190;

            if (rbQR.Checked)
            {
                // Chế độ QR Code: Hai ô sát mép, cách lề 15px
                int startX1 = 30;
                int startX2 = this.ClientSize.Width - barcodeWidth - 30;

                if (picBarcode1 != null)
                {
                    picBarcode1.Size = new Size(barcodeWidth, barcodeHeight);
                    picBarcode1.Location = new Point(startX1, startY);
                }
                if (lblBarcode1 != null)
                {
                    lblBarcode1.Size = new Size(barcodeWidth, labelHeight);
                    lblBarcode1.Location = new Point(startX1, startY + barcodeHeight + 5);
                }

                if (picBarcode2 != null)
                {
                    picBarcode2.Size = new Size(barcodeWidth, barcodeHeight);
                    picBarcode2.Location = new Point(startX2, startY);
                    picBarcode2.Visible = true;
                }
                if (lblBarcode2 != null)
                {
                    lblBarcode2.Size = new Size(barcodeWidth, labelHeight);
                    lblBarcode2.Location = new Point(startX2, startY + barcodeHeight + 5);
                    lblBarcode2.Visible = rbQR.Checked && !string.IsNullOrEmpty(lblBarcode2.Text);
                    Console.WriteLine($"UpdateBarcodeLayout - lblBarcode2: Location=({lblBarcode2.Location.X},{lblBarcode2.Location.Y}), Visible={lblBarcode2.Visible}, Text={lblBarcode2.Text}");
                }
            }
            else
            {
                // Chế độ DataMatrix: Căn giữa ô duy nhất
                int startX = (this.ClientSize.Width - barcodeWidth) / 2;

                if (picBarcode1 != null)
                {
                    picBarcode1.Size = new Size(barcodeWidth, barcodeHeight);
                    picBarcode1.Location = new Point(startX, startY);
                }
                if (lblBarcode1 != null)
                {
                    lblBarcode1.Size = new Size(barcodeWidth, labelHeight);
                    lblBarcode1.Location = new Point(startX, startY + barcodeHeight + 5);
                }

                if (picBarcode2 != null)
                {
                    picBarcode2.Size = new Size(0, 0);
                    picBarcode2.Location = new Point(0, 0);
                    picBarcode2.Visible = false;
                }
                if (lblBarcode2 != null)
                {
                    lblBarcode2.Size = new Size(0, 0);
                    lblBarcode2.Location = new Point(0, 0);
                    lblBarcode2.Visible = false;
                }
            }

            if (lblSavePath != null)
            {
                lblSavePath.Size = new Size(400, 45);
                lblSavePath.Location = new Point(25, 135);
            }

            if (lblAuthor != null)
            {
                lblAuthor.Location = new Point(this.ClientSize.Width - lblAuthor.Width - 5, 405);
            }
        }

        // Thiết lập sự kiện cho các nút
        private void SetupEvents()
        {
            txtInput.TextChanged += TxtInput_TextChanged; // Thêm sự kiện TextChanged cho txtInput
            btnSave.Click += BtnSave_Click;
            btnReset.Click += BtnReset_Click;
            rbQR.CheckedChanged += BarcodeTypeChanged;
            rbDataMatrix.CheckedChanged += BarcodeTypeChanged;
            // Thêm sự kiện Paint cho các PictureBox
            picBarcode1.Paint += PicBarcode_Paint;
            picBarcode2.Paint += PicBarcode_Paint;
        }

        // Xử lý khi thay đổi loại mã (QR/DataMatrix)
        private void BarcodeTypeChanged(object sender, EventArgs e)
        {
            if (picBarcode1.Image != null)
            {
                picBarcode1.Image.Dispose();
                picBarcode1.Image = null;
            }
            if (picBarcode2.Image != null)
            {
                picBarcode2.Image.Dispose();
                picBarcode2.Image = null;
            }
            lblBarcode1.Text = "";
            lblBarcode2.Text = "";
            picBarcode2.Visible = rbQR.Checked;
            lblBarcode2.Visible = rbQR.Checked;
            UpdateBarcodeLayout();
            this.Invalidate();
            this.Refresh();
            Console.WriteLine($"BarcodeTypeChanged - rbQR.Checked: {rbQR.Checked}, lblBarcode2.Visible: {lblBarcode2.Visible}");

            // Generate barcode immediately after type change if input is not empty
            if (!string.IsNullOrEmpty(txtInput.Text))
            {
                GenerateBarcode();
            }
        }

        // Xử lý khi văn bản trong ô nhập liệu thay đổi
        private void TxtInput_TextChanged(object sender, EventArgs e)
        {
            GenerateBarcode();
        }

        // Hàm tạo mã vạch (tách ra từ BtnGenerate_Click)
        private void GenerateBarcode()
        {
            if (string.IsNullOrEmpty(txtInput.Text))
            {
                // Clear barcodes if input is empty
                if (generatedBarcode1 != null)
                {
                    generatedBarcode1.Dispose();
                    generatedBarcode1 = null;
                }
                if (generatedBarcode2 != null)
                {
                    generatedBarcode2.Dispose();
                    generatedBarcode2 = null;
                }
                picBarcode1.Image = null;
                picBarcode2.Image = null;
                lblBarcode1.Text = "";
                lblBarcode2.Text = "";
                picBarcode2.Visible = rbQR.Checked;
                lblBarcode2.Visible = rbQR.Checked;
                UpdateBarcodeLayout();
                this.Invalidate();
                this.Refresh();
                return;
            }

            if (!rbQR.Checked && !rbDataMatrix.Checked)
            {
                MessageBox.Show("Vui lòng chọn loại mã (QR hoặc DataMatrix)!", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var baseData = txtInput.Text.Trim();
                var format = rbQR.Checked ? BarcodeFormat.QR_CODE : BarcodeFormat.DATA_MATRIX;

                var writer = new BarcodeWriterPixelData
                {
                    Format = format,
                    Options = new EncodingOptions
                    {
                        Height = 150,
                        Width = 150,
                        Margin = 2
                    }
                };

                if (rbQR.Checked)
                {
                    var data1 = baseData + "#1";
                    var pixelData1 = writer.Write(data1);
                    generatedBarcode1 = ConvertToCircularQRCode(pixelData1);
                    picBarcode1.Image = generatedBarcode1;
                    lblBarcode1.Text = data1;
                    lblBarcode1.Visible = true;
                    lblBarcode1.Refresh(); // Thêm dòng này

                    var data2 = baseData + "#2";
                    var pixelData2 = writer.Write(data2);
                    generatedBarcode2 = ConvertToCircularQRCode(pixelData2);
                    picBarcode2.Image = generatedBarcode2;
                    lblBarcode2.Text = data2;
                    picBarcode2.Visible = true;
                    lblBarcode2.Visible = true;
                    //lblBarcode2.Refresh(); // Thêm dòng này

                    Console.WriteLine($"lblBarcode2.Text: {lblBarcode2.Text}, Visible: {lblBarcode2.Visible}");
                }
                else
                {
                    var pixelData = writer.Write(baseData);
                    generatedBarcode1 = ConvertToCircularQRCode(pixelData);
                    picBarcode1.Image = generatedBarcode1;
                    lblBarcode1.Text = baseData;
                    lblBarcode1.Visible = true;
                    //lblBarcode1.Refresh(); // Thêm dòng này

                    picBarcode2.Visible = false;
                    lblBarcode2.Visible = false;
                    lblBarcode2.Text = "";
                }

                UpdateBarcodeLayout();
                this.Invalidate();
                this.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo mã: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Xử lý khi nhấn nút "Lưu ảnh"
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (generatedBarcode1 == null)
            {
                MessageBox.Show("Chưa có mã nào được tạo!", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (rbQR.Checked && generatedBarcode2 != null)
                {
                    SaveQRPair();
                }
                else
                {
                    SaveSingleBarcode();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Lưu cặp mã QR
        private void SaveQRPair()
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string baseName = MakeValidFileName(txtInput.Text.Trim());
                    string path1 = Path.Combine(fbd.SelectedPath, $"{baseName}#1.png");
                    string path2 = Path.Combine(fbd.SelectedPath, $"{baseName}#2.png");

                    SaveWithText(generatedBarcode1, lblBarcode1.Text, path1);
                    if (generatedBarcode2 != null)
                    {
                        SaveWithText(generatedBarcode2, lblBarcode2.Text, path2);
                        lblSavePath.Text = $"Đã lưu:\n{path1}\n{path2}";
                    }
                    else
                    {
                        lblSavePath.Text = $"Đã lưu:\n{path1}\n(Lưu mã #2 thất bại)";
                    }
                }
            }
        }

        // Lưu một mã duy nhất (DataMatrix)
        private void SaveSingleBarcode()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                string fileName = MakeValidFileName(txtInput.Text.Trim());
                sfd.FileName = fileName;
                sfd.Filter = "PNG Image|*.png";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    SaveWithText(generatedBarcode1, lblBarcode1.Text, sfd.FileName);
                    lblSavePath.Text = "Đã lưu: " + sfd.FileName;
                }
            }
        }

        // Lưu ảnh mã với văn bản bên dưới
        private void SaveWithText(Bitmap barcode, string text, string filePath)
        {
            int labelHeight = 40;
            using (Bitmap labeledBitmap = new Bitmap(barcode.Width, barcode.Height + labelHeight))
            using (Graphics g = Graphics.FromImage(labeledBitmap))
            {
                g.Clear(Color.White);
                g.DrawImage(barcode, 0, 0);
                using (Font font = new Font("Arial", 12, FontStyle.Bold))
                using (StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    g.DrawString(text, font, Brushes.Black,
                        new RectangleF(0, barcode.Height, barcode.Width, labelHeight), format);
                }
                labeledBitmap.Save(filePath, ImageFormat.Png);
            }
        }

        // Xử lý khi nhấn nút "Reset"
        private void BtnReset_Click(object sender, EventArgs e)
        {
            try
            {
                if (picBarcode1.Image != null)
                {
                    picBarcode1.Image.Dispose();
                    picBarcode1.Image = null;
                }
                if (picBarcode2.Image != null)
                {
                    picBarcode2.Image.Dispose();
                    picBarcode2.Image = null;
                }

                lblBarcode1.Text = "";
                lblBarcode2.Text = "";
                txtInput.Text = "";
                txtInput.Focus();

                if (generatedBarcode1 != null)
                {
                    generatedBarcode1.Dispose();
                    generatedBarcode1 = null;
                }
                if (generatedBarcode2 != null)
                {
                    generatedBarcode2.Dispose();
                    generatedBarcode2 = null;
                }

                lblSavePath.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi reset: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Tạo tên tệp hợp lệ từ dữ liệu nhập
        private string MakeValidFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return Regex.Replace(name, invalidRegStr, "_");
        }

        // Đặt văn bản gợi ý cho ô nhập liệu
        private void SetPlaceholderText(TextBox textBox, string placeholder)
        {
            SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, placeholder);
        }

        // Chuyển dữ liệu pixel thành mã QR hình tròn
        private Bitmap ConvertToCircularQRCode(ZXing.Rendering.PixelData pixelData)
        {
            if (pixelData == null || pixelData.Width <= 0 || pixelData.Height <= 0)
            {
                throw new ArgumentException("Dữ liệu pixel không hợp lệ.");
            }

            Bitmap circularQRCode = new Bitmap(pixelData.Width, pixelData.Height);

            using (Graphics g = Graphics.FromImage(circularQRCode))
            {
                g.Clear(Color.White);
                float dotSize = pixelData.Width / 100f;
                for (int y = 0; y < pixelData.Height; y++)
                {
                    for (int x = 0; x < pixelData.Width; x++)
                    {
                        int index = (y * pixelData.Width + x) * 4;
                        if (index + 3 < pixelData.Pixels.Length && pixelData.Pixels[index] == 0)
                        {
                            g.FillEllipse(Brushes.Black, x - dotSize / 2, y - dotSize / 2, dotSize, dotSize);
                        }
                    }
                }
            }

            return circularQRCode;
        }
    }
}