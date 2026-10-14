using System.Collections.Generic;

namespace BlockBlast
{
    // Chỉ sửa nội dung ở file này khi nhóm muốn cập nhật thông tin Credits.
    internal static class CreditsContent
    {
        public const string TeamName = "C#LEGENDS";
        public const string ProjectName = "Block Blast";
        public const string TeamIntroduction =
            "Trò chơi xếp khối trên máy tính, kết hợp giải đố với các vật phẩm hỗ trợ.";

        // Tên và vai trò lấy từ textBox1 trong bản dự án đã gửi.
        // Kiểm lại với nhóm và thay các giá trị MSSV trước khi nộp bài.
        public static readonly List<CreditMember> Members = new List<CreditMember>
        {
            new CreditMember("Văn Thanh", "51.01.104.092", "Logic game"),
            new CreditMember("Tuấn Hưng", "51.01.104.038", "Logic game"),
            new CreditMember("Quang Hưng", "51.01.104.037", "Giao diện"),
            new CreditMember("Tấn Phát", "51.01.104.069", "Giao diện")
        };

        // Chưa có thông tin xác nhận về động lực thật của nhóm.
        // Có thể viết về lý do chọn game, mục tiêu thực hành và trải nghiệm mong muốn.
        public static readonly CreditsSection[] Motivation =
        {
            new CreditsSection("Vì sao nhóm chọn dự án này?",
                "[Điền lý do thực tế nhóm chọn phát triển trò chơi xếp khối. " +
                "Ví dụ: yêu thích thể loại giải đố, muốn tự xây dựng một game trên máy tính.]") ,
            new CreditsSection("Mục tiêu phát triển",
                "[Điền mục tiêu nhóm đã thống nhất. Ví dụ: vận dụng kiến thức C#, " +
                "lập trình hướng đối tượng, xử lý sự kiện và phối hợp làm việc qua GitHub.]") ,
            new CreditsSection("Trải nghiệm nhóm muốn mang đến",
                "[Điền trải nghiệm nhóm hướng tới. Ví dụ: luật chơi dễ tiếp cận, " +
                "có thử thách tăng dần và thêm lựa chọn chiến thuật nhờ vật phẩm.]")
        };

        // Hướng dẫn mô tả phần xử lý thực tế trong bản code đã kiểm tra.
        public static readonly CreditsSection[] Guide =
        {
            new CreditsSection("1. Mục tiêu và luật chơi",
                "Đặt các khối vào bàn 8×8 để lấp đầy hàng hoặc cột và ghi điểm. " +
                "Một hàng hoặc cột đầy sẽ được xóa để tạo thêm chỗ trống. " +
                "Các ô trong hàng hoặc cột không cần cùng màu để được xóa."),
            new CreditsSection("2. Đặt khối bằng chuột",
                "Nhấn giữ chuột trái trên một khối trong khay, kéo lên bàn và thả " +
                "tại vị trí hợp lệ. Khối phải nằm trong bàn và không chồng lên ô đã có gạch. " +
                "Khi cả ba khay đều trống, game sinh bộ khối mới."),
            new CreditsSection("3. Vật phẩm Đổi Gạch",
                "Bấm Đổi Gạch khi bạn còn vật phẩm và có ít nhất một khay trống. " +
                "Mỗi lần sử dụng thêm một khối vào một khay trống. " +
                "Nếu cả ba khay đang đầy, vật phẩm không bị trừ."),
            new CreditsSection("4. Ngắm và bắn vật phẩm",
                "Với Bom 3×3, Tia Chữ Thập, Sấm Sét và Trọng Lực: nhấn giữ chuột trái " +
                "trên vật phẩm, kéo ná để ngắm rồi thả. Hướng bắn ngược với hướng kéo; " +
                "hãy quan sát đường ngắm trước khi thả. Trong ván chơi thông thường, " +
                "bắn hụt vẫn tiêu hao vật phẩm. Tránh giữ ná ở độ căng tối đa quá lâu: " +
                "dây có thể đứt, làm mất vật phẩm đang cầm và thêm một vật phẩm còn trong kho, nếu có."),
            new CreditsSection("5. Tác dụng của từng vật phẩm",
                "• Bom 3×3: xóa các ô có gạch trong vùng 3×3 quanh điểm trúng.\r\n" +
                "• Tia Chữ Thập: xóa hàng và cột đi qua điểm trúng.\r\n" +
                "• Sấm Sét: bắn vào một ô có gạch để xóa các ô cùng màu.\r\n" +
                "• Trọng Lực: bắn vào bàn để dồn các ô xuống đáy; các dòng đầy sau đó được xử lý."),
            new CreditsSection("6. Nhận vật phẩm từ combo",
                "Combo được xét theo các dòng xóa trong cùng một lần.\r\n" +
                "• Hai dòng song song: nhận Đổi Gạch.\r\n" +
                "• Một hàng và một cột: nhận Tia Chữ Thập và tích lũy một lần rung chấn.\r\n" +
                "• Ba dòng: nhận Bom 3×3 và tích lũy một lần rung chấn.\r\n" +
                "• Bốn dòng trở lên: nhận Sấm Sét và tích lũy một lần rung chấn.\r\n" +
                "• Đủ ba lần rung chấn: nhận thêm Trọng Lực."),
            new CreditsSection("7. Hướng dẫn tương tác",
                "Game có tám màn hướng dẫn: đặt khối, tạo combo và thực hành năm loại vật phẩm. " +
                "Ở những màn có vị trí đặt bắt buộc, hãy làm theo chỉ dẫn trên màn hình. " +
                "Các lượt thử vật phẩm trong hướng dẫn được xử lý riêng với ván chơi thông thường."),
            new CreditsSection("8. Tạm dừng và kết thúc ván",
                "Dùng nút menu trong game để tạm dừng, tiếp tục, chơi lại hoặc bật/tắt âm thanh. " +
                "Game xét kết thúc khi các khối còn lại không thể đặt và không còn vật phẩm " +
                "được tính là có thể sử dụng. Hãy theo dõi chỗ trống và kho vật phẩm trước mỗi lượt.")
        };

        // Chỉ trình bày các cơ chế tìm thấy trong code, không lấy tính năng kế hoạch làm tính năng đã có.
        public static readonly CreditsSection[] Innovation =
        {
            new CreditsSection("1. Thưởng vật phẩm theo combo",
                "Các kiểu xóa nhiều dòng mang lại phần thưởng khác nhau. " +
                "Người chơi có thêm mục tiêu khi lựa chọn vị trí đặt khối và quản lý vật phẩm."),
            new CreditsSection("2. Kéo ná để bắn vật phẩm",
                "Vật phẩm tác động lên bàn thông qua thao tác kéo, ngắm và thả. " +
                "Cơ chế này bổ sung lựa chọn điểm tác động và yêu cầu cân nhắc thời điểm sử dụng."),
            new CreditsSection("3. Hướng dẫn bằng tám màn thực hành",
                "Người mới thực hành trực tiếp từ thao tác đặt khối, tạo combo đến sử dụng vật phẩm, " +
                "thay vì chỉ đọc luật chơi."),
            new CreditsSection("4. Điều chỉnh cách sinh khối theo điểm",
                "Game thay đổi cách chọn các nhóm khối dễ, vừa và khó theo các mốc điểm. " +
                "Khi điểm tăng, người chơi cần cân nhắc việc bố trí bàn và sử dụng vật phẩm.")
        };
    }

    internal sealed class CreditMember
    {
        public string FullName { get; private set; }
        public string StudentId { get; private set; }
        public string Role { get; private set; }

        public CreditMember(string fullName, string studentId, string role)
        {
            FullName = fullName;
            StudentId = studentId;
            Role = role;
        }
    }

    internal sealed class CreditsSection
    {
        public string Title { get; private set; }
        public string Body { get; private set; }

        public CreditsSection(string title, string body)
        {
            Title = title;
            Body = body;
        }
    }
}
