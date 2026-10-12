using System;
using System.Drawing;

namespace BlockBlast.Module
{
    // 1. Danh sách 5 loại Item tương ứng với mốc Combo n = 2 đến 6
    public enum ItemType
    {
        RerollDock = 2,     // n=2: Đổi 3 khối gạch mới trên Dock
        Bomb3x3 = 3,        // n=3: Ném bom nổ vùng 3x3
        CrossClear = 4,     // n=4: Bắn tia nổ 1 hàng + 1 cột
        ColorLightning = 5, // n=5: Sấm sét quét sạch tất cả ô cùng màu
        GravityFall = 6     // n=6: Dồn khối rơi xuống đáy
    }

    // 2. Khuôn mẫu (Bản thiết kế) cho 1 món đồ Item
    public class Item
    {
        // Các thông tin của Item
        public ItemType Type { get; private set; }   // Loại item
        public string Name { get; private set; }       // Tên item (ví dụ: "Bom 3x3")
        public string Description { get; private set; }// Hướng dẫn sử dụng
        public int Count { get; set; }                 // Số lượng đang có trong kho
        public Color ThemeColor { get; private set; }  // Màu sắc đại diện

        // Hàm đúc (Constructor): chạy mỗi khi tạo một Item mới
        public Item(ItemType type, string name, string description, Color color, int initialCount = 0)
        {
            this.Type = type;
            this.Name = name;
            this.Description = description;
            this.ThemeColor = color;
            this.Count = initialCount;
        }

        // Hành động 1: Khi ăn Combo thì được cộng thêm đồ
        public void Add(int amount = 1)
        {
            Count += amount;
        }

        // Hành động 2: Khi người chơi kéo thả dùng đồ thì trừ đi 1 cái
        public bool TryConsume()
        {
            if (Count > 0)
            {
                Count--;
                return true; // Dùng thành công
            }
            return false; // Hết đồ rồi, không dùng được
        }
    }
}