namespace BlockBlast
{
    // Nội dung chung của Credits. Thông tin thành viên vẫn lấy từ CreditsContent.cs.
    // Dùng với CreditsSection đã có trong bộ code Credits bốn tab.
    internal static class CreditsNarrative
    {
        public static readonly CreditsSection[] Motivation =
        {
            new CreditsSection("Mỗi khối nhỏ, một quyết định lớn",
                "Một bàn cờ nhỏ vẫn có thể tạo ra nhiều lựa chọn. Đặt được một khối là bước đầu; " +
                "đặt ở đâu để giữ chỗ cho những khối tiếp theo mới là thử thách. " +
                "Block Blast được phát triển với mong muốn biến những thao tác kéo và thả quen thuộc " +
                "thành trải nghiệm đòi hỏi quan sát, cân nhắc và thử những cách giải quyết khác nhau."),

            new CreditsSection("Từ ý tưởng đến một trò chơi có thể trải nghiệm",
                "Dự án là cơ hội để nhóm vận dụng kiến thức lập trình vào một sản phẩm tương tác. " +
                "Từ bàn cờ, khối gạch và cách tính điểm đến giao diện và âm thanh, mỗi thành phần " +
                "đều phải phối hợp để tạo nên một ván chơi. Qua đó, nhóm hướng tới việc hiểu sâu hơn " +
                "về lập trình hướng đối tượng, xử lý sự kiện và cách tổ chức mã nguồn."),

            new CreditsSection("Quen thuộc để dễ bắt đầu, có chiều sâu để tiếp tục",
                "Luật chơi được giữ đơn giản: đặt khối, hoàn thành hàng hoặc cột và tạo thêm khoảng trống. " +
                "Các vật phẩm và thao tác ngắm bắn bổ sung lựa chọn khi bàn cờ trở nên khó xử lý. " +
                "Mục tiêu của nhóm là tạo một trò chơi dễ tiếp cận, đồng thời khuyến khích người chơi " +
                "suy nghĩ về cả nước đi hiện tại lẫn những lượt tiếp theo."),

            new CreditsSection("Cùng xây dựng và cùng hoàn thiện",
                "Phát triển game cũng là quá trình học cách phối hợp: chia nhiệm vụ, nối các phần công việc " +
                "và trao đổi về những điểm cần cải thiện. Nhóm sử dụng GitHub để quản lý mã nguồn và " +
                "từng bước hoàn thiện sản phẩm. Mỗi lần chỉnh sửa hướng tới một mục tiêu chung: " +
                "giúp người chơi hiểu thao tác và cảm nhận rõ kết quả của lựa chọn mình vừa thực hiện.")
        };

        public static readonly CreditsSection[] Guide =
        {
            new CreditsSection("1. Bàn cờ, khối gạch và mục tiêu",
                "Bạn đặt các khối vào bàn cờ 8×8 để hoàn thành hàng hoặc cột và ghi điểm. " +
                "Khi một hàng hoặc cột được lấp đầy, các ô trên dòng đó được xóa để tạo chỗ cho lượt tiếp theo. " +
                "Các ô không cần cùng màu để hoàn thành một dòng."),

            new CreditsSection("2. Kéo, đặt và giữ khoảng trống",
                "Nhấn giữ chuột trái trên một khối trong khay, kéo lên bàn rồi thả tại vị trí hợp lệ. " +
                "Khối phải nằm trong bàn cờ và không chồng lên các ô đã có gạch. " +
                "Khi cả ba khay đều trống, game sinh một bộ khối mới. " +
                "Hãy quan sát cả ba khối trước khi chọn khối đặt đầu tiên."),

            new CreditsSection("3. Combo mang lại vật phẩm",
                "Combo ở đây được xét theo các dòng xóa trong cùng một lần:\r\n" +
                "• Hai hàng hoặc hai cột song song: nhận Đổi Gạch.\r\n" +
                "• Một hàng và một cột: nhận Tia Chữ Thập và tích lũy một lần rung chấn.\r\n" +
                "• Ba dòng: nhận Bom 3×3 và tích lũy một lần rung chấn.\r\n" +
                "• Bốn dòng trở lên: nhận Sấm Sét và tích lũy một lần rung chấn.\r\n" +
                "• Tích lũy đủ ba lần rung chấn: nhận thêm Trọng Lực."),

            new CreditsSection("4. Đổi Gạch: bổ sung một lựa chọn",
                "Khi còn vật phẩm Đổi Gạch và có ít nhất một khay trống, bấm vào vật phẩm để " +
                "thêm một khối mới vào một khay trống. Mỗi lần sử dụng bổ sung một khối. " +
                "Nếu cả ba khay đang đầy, thao tác này không tiêu hao vật phẩm."),

            new CreditsSection("5. Kéo ná và chọn điểm tác động",
                "Bom 3×3, Tia Chữ Thập, Sấm Sét và Trọng Lực dùng thao tác ngắm bắn. " +
                "Nhấn giữ chuột trái trên vật phẩm, kéo ná và quan sát đường ngắm, rồi thả tay để bắn. " +
                "Hướng bắn ngược với hướng kéo. Một cú kéo quá nhẹ có thể được hủy trước khi bắn. " +
                "Trong ván chơi thông thường, bắn hụt vẫn tiêu hao vật phẩm. " +
                "Với Sấm Sét, hãy ngắm vào một ô đang có gạch thay vì ô trống."),

            new CreditsSection("6. Chọn đúng công cụ cho tình huống",
                "• Bom 3×3: xóa các ô có gạch trong vùng 3×3 quanh điểm trúng; thích hợp khi muốn mở một vùng trống.\r\n" +
                "• Tia Chữ Thập: xóa hàng và cột đi qua điểm trúng; hãy cân nhắc giao điểm cần tác động.\r\n" +
                "• Sấm Sét: trúng một ô có gạch để xóa các ô cùng màu trên bàn.\r\n" +
                "• Trọng Lực: bắn vào bàn để dồn các ô xuống đáy; game tiếp tục xử lý những dòng đầy hình thành sau đó."),

            new CreditsSection("7. Đừng giữ ná căng quá lâu",
                "Khi kéo mạnh, điểm ngắm có thể rung nhẹ. Giữ ná ở độ căng tối đa quá lâu có thể làm đứt dây. " +
                "Trong ván chơi thông thường, sự cố này làm mất vật phẩm đang cầm và thêm một vật phẩm " +
                "còn trong kho, nếu có. Hãy quan sát đường ngắm và thả tay kịp thời. " +
                "Ở các màn hướng dẫn vật phẩm, bạn có thể thử lại khi thao tác chưa thành công."),

            new CreditsSection("8. Học qua tám màn thực hành",
                "Hệ thống hướng dẫn gồm tám màn, từ đặt khối và tạo combo đến làm quen với năm loại vật phẩm. " +
                "Làm theo chỉ dẫn trên màn hình; một số màn yêu cầu đặt khối vào vị trí xác định. " +
                "Hoàn thành các màn thực hành để tiếp tục sang phần chơi thông thường."),

            new CreditsSection("9. Tạm dừng và kết thúc ván",
                "Nút menu trong ván chơi cho phép tạm dừng, tiếp tục, chơi lại hoặc bật/tắt âm thanh. " +
                "Game kiểm tra khả năng đặt các khối còn lại và vật phẩm có thể sử dụng trước khi báo kết thúc ván. " +
                "Nếu khối đang có không thể đặt nhưng bạn vẫn còn công cụ cứu tình huống, hãy xem lại kho vật phẩm."),

            new CreditsSection("10. Một vài gợi ý khi chơi",
                "Quan sát cả bộ khối trước khi đặt. Giữ lại những khoảng trống có thể dùng cho nhiều hình khối. " +
                "Tìm cơ hội xóa nhiều dòng trong cùng một lượt để nhận vật phẩm. " +
                "Khi dùng vật phẩm, cân nhắc khoảng trống tạo được và những khối sẽ đặt tiếp theo. " +
                "Một nước đi tốt giúp bạn có thêm lựa chọn ở lượt sau.")
        };

        public static readonly CreditsSection[] Innovation =
        {
            new CreditsSection("1. Combo trở thành một nguồn tài nguyên",
                "Xóa nhiều dòng đem lại cả điểm số lẫn vật phẩm. Các kiểu combo khác nhau tạo phần thưởng " +
                "khác nhau; một số combo còn tích lũy rung chấn để nhận Trọng Lực. " +
                "Cơ chế này kết nối cách đặt khối với việc chuẩn bị công cụ cho những tình huống về sau."),

            new CreditsSection("2. Kết hợp xếp khối với ngắm bắn",
                "Bên cạnh đặt khối, người chơi sử dụng ná để chọn điểm tác động của vật phẩm. " +
                "Hướng kéo, thời điểm thả và vị trí ngắm cùng ảnh hưởng đến thao tác. " +
                "Khả năng bắn hụt hoặc đứt dây tạo thêm yếu tố cân nhắc khi sử dụng công cụ hỗ trợ."),

            new CreditsSection("3. Làm quen bằng hành động",
                "Tám màn hướng dẫn tạo các tình huống cụ thể để người chơi tự đặt khối, tạo combo " +
                "và thử vật phẩm. Những thao tác mới được giới thiệu từng bước, " +
                "giúp người chơi quan sát kết quả ngay trên bàn cờ."),

            new CreditsSection("4. Thử thách thay đổi theo điểm số",
                "Cách chọn các nhóm khối dễ, vừa và khó thay đổi theo các mốc điểm. " +
                "Khi điểm tăng, người chơi cần chú ý hơn đến cách giữ khoảng trống và phối hợp vật phẩm. " +
                "Phần cải tiến tập trung vào việc thay đổi tình huống cần giải quyết trong quá trình chơi."),

            new CreditsSection("5. Phản hồi giúp nhìn rõ kết quả",
                "Hoạt họa xóa dòng, chuyển động của vật phẩm, âm thanh và hiệu ứng rung phản hồi các hành động " +
                "trên bàn cờ. Những phản hồi này giúp người chơi nhận biết combo, " +
                "cú bắn và tác động của vật phẩm trong từng tình huống.")
        };
    }
}
