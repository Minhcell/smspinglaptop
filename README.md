# SmsPing (bản Laptop)

## Cách build DỄ NHẤT - không cần cài Visual Studio, build qua GitHub (giống cách build app Android)

### Bước 1: Đưa code lên GitHub
1. Vào https://github.com , đăng nhập tài khoản của anh
2. Bấm dấu **+** góc trên bên phải > **New repository**
3. Đặt tên (VD: `vtv-sms-ping-laptop`) > bấm **Create repository**
4. Trong trang repo vừa tạo, bấm **"uploading an existing file"** (hoặc Add file > Upload files)
5. Giải nén file zip em gửi ra máy, kéo thả **toàn bộ nội dung bên trong thư mục `SmsPing`** (không kéo cả thư mục cha) vào khung upload — kéo cả file lẫn các thư mục con như `.github`
6. Bấm **Commit changes** ở dưới để lưu lên GitHub

### Bước 2: Để GitHub tự build file .exe
1. Vào tab **Actions** ở trên cùng repo
2. Nếu thấy dòng "Build EXE" đang chạy (chấm vàng xoay) → đợi 2-3 phút cho xong (chấm xanh ✅)
3. Nếu không thấy tự chạy: bấm vào **Build EXE** ở khung bên trái > bấm nút **Run workflow** > **Run workflow** lần nữa để xác nhận

### Bước 3: Tải file .exe về
1. Sau khi thấy dấu ✅ xanh, bấm vào lần chạy đó (dòng có ✅)
2. Kéo xuống dưới cùng trang, thấy mục **Artifacts** > bấm vào **SmsPing-exe** để tải về (file .zip)
3. Giải nén ra, bên trong có file **SmsPing.exe**

### Bước 4: Chạy trên laptop
1. Copy cả thư mục vừa giải nén (không chỉ file .exe, cần đủ các file .dll đi kèm) vào laptop muốn dùng
2. Double-click **SmsPing.exe** để chạy
3. Nếu Windows báo "Windows protected your PC" → bấm **More info** > **Run anyway** (do file chưa có chữ ký số, bình thường với app tự build)
4. Laptop cần đã cài **.NET Framework 4.8** — Windows 10/11 hầu hết đã có sẵn; nếu app báo thiếu, tải tại: https://dotnet.microsoft.com/download/dotnet-framework/net48

## Mỗi khi sửa code
Chỉ cần sửa file trực tiếp trên GitHub (bấm vào file > biểu tượng bút chì ✏️ > sửa > Commit), Actions sẽ tự build lại, quay lại Bước 2-3 để tải bản .exe mới.

## Lưu ý
App cần cắm modem GSM qua cổng COM/USB thật thì nút Connect mới hoạt động được. Chưa có thiết bị vẫn mở app bình thường để xem giao diện.
