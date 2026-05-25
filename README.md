# SmartHome

SmartHome gồm giao diện React, API ASP.NET, Mosquitto MQTT broker và firmware cho
các thiết bị. Cấu hình Docker Compose ở thư mục gốc dành cho một máy chủ Linux
trong mạng LAN.

## Chạy bằng Docker Compose

Yêu cầu: Docker Engine và Docker Compose v2.

Tạo file biến môi trường và đặt hai mật khẩu khác nhau, đủ mạnh:

```bash
cp .env.example .env
openssl rand -hex 32
openssl rand -hex 32
```

Điền hai giá trị vừa tạo vào `MOSQUITTO_DYNSEC_ADMIN_PASSWORD` và
`MOSQUITTO_BACKEND_PASSWORD` trong `.env`. Không commit file này.

```bash
docker compose up -d --build
docker compose ps
```

Sau khi khởi động:

- Giao diện web: `http://<ip-may-chu>/` (mặc định cổng `80`). Có thể đổi cổng
  host bằng biến `SMARTHOME_HTTP_PORT`, ví dụ
  `SMARTHOME_HTTP_PORT=8080 docker compose up -d`.
- MQTT cho thiết bị trong LAN: `<ip-may-chu>:1883` hoặc
  `smarthome-broker.local:1883` sau khi cấu hình Avahi bên dưới.
- Backend không publish cổng trực tiếp. Nginx chuyển tiếp `/api/` tới backend và
  được cấu hình để không buffer luồng SSE.

Các service backend và broker lưu dữ liệu trong named volume. Xem log bằng:

```bash
docker compose logs -f backend broker
```

## Mosquitto Dynamic Security

Compose khởi tạo file Dynamic Security trong volume `mosquitto-data`. Khi
backend bắt đầu chạy, tài khoản, role và ACL được đồng bộ qua control topic của
Mosquitto:

| Đối tượng | Định danh | Quyền |
| --- | --- | --- |
| Quản trị | `smarthome-admin` mặc định | Chỉ dùng cho Dynamic Security và kiểm tra broker; backend dùng kết nối riêng này để tạo, cập nhật, xóa tài khoản thiết bị. |
| Backend vận hành | `smarthome-backend`, client ID `server` | Publish lệnh/provision response và subscribe các topic trạng thái, availability, command result. Không có quyền quản trị. |
| Thiết bị đã đăng ký | username và client ID đều là `deviceId` | Chỉ publish/subscribe `home/devices/<deviceId>/...` của chính nó. Mật khẩu là `AccessToken` được cấp khi claim. |
| Thiết bị đang provision | anonymous, client ID là MAC chuẩn hóa | Chỉ dùng `home/provision/<clientId>/request` và `response`. |

Khi claim thiết bị, backend tạo hoặc cập nhật client Mosquitto trước khi gửi
credential cho firmware. Reprovision và xóa thiết bị sẽ thu hồi client. Thiết kế
này dành cho deployment mới; tài khoản MQTT của thiết bị được tạo từ luồng claim.

Có thể xem danh sách client (lệnh sẽ hỏi mật khẩu admin; sửa username nếu đã đổi):

```bash
docker compose exec broker mosquitto_ctrl -h localhost -u smarthome-admin dynsec listClients
```

Mật khẩu admin trong `.env` chỉ được dùng để tạo file Dynamic Security ở lần
đầu. Không đổi riêng giá trị này sau khi volume đã tồn tại, vì broker healthcheck
và backend sẽ không đăng nhập được. Hãy đổi mật khẩu trong Mosquitto trước, rồi
cập nhật `.env`. Mật khẩu backend được backend đồng bộ lại từ `.env` khi khởi
động.

`listener_allow_anonymous` vẫn bật để firmware có thể provision, nhưng anonymous
được gán vào role giới hạn theo `%c`; đây không phải xác minh danh tính phần
cứng vì client ID/MAC có thể bị giả mạo. Quan trọng hơn, cổng `1883` không mã
hóa nên mật khẩu và payload đi qua LAN ở dạng có thể quan sát. Không mở/NAT cổng
này ra Internet; nên bổ sung TLS trên `8883` trước khi dùng trong mạng không
đáng tin cậy. Dynamic Security cung cấp xác thực và phân quyền, không thay thế
TLS.

Tham khảo thêm [tài liệu Dynamic Security chính thức của
Mosquitto](https://mosquitto.org/documentation/dynamic-security/).

## SQLite

Trong container, backend dùng connection string:

```text
Data Source=/app/data/smarthome.db
```

Thư mục `/app/data` được gắn vào named volume `sqlite-data`, nên database không
mất khi container được tạo lại. Backend tự áp dụng EF Core migration trước khi
nhận request. Chỉ nên chạy một replica backend khi còn sử dụng SQLite.

### Khởi tạo lại toàn bộ dữ liệu

Deployment này không nhập file `server/Presentation/smarthome.db` cũ. Khi cần
xóa sạch cả SQLite lẫn cấu hình/tài khoản Mosquitto để bắt đầu lại:

```bash
docker compose down -v
docker compose up -d --build
```

Lệnh `down -v` xóa vĩnh viễn named volume `sqlite-data` và `mosquitto-data`.
Service `backend-data-init` sẽ tạo quyền sở hữu phù hợp cho volume SQLite mới.
Các thiết bị đã lưu credential từ stack trước phải được đưa về provisioning và
claim lại.

### Sao lưu

Để sao lưu nhất quán, dừng ghi trước rồi copy toàn bộ thư mục SQLite (bao gồm
các file `-wal`/`-shm` nếu còn) ra host:

```bash
mkdir -p backups/sqlite
docker compose stop backend
docker compose cp backend:/app/data/. ./backups/sqlite/
docker compose start backend
```

## Avahi và `smarthome-broker.local`

Avahi chạy trực tiếp trên máy chủ Linux, không chạy trong container. mDNS sử
dụng multicast của mạng LAN; đặt Avahi trong Docker bridge sẽ không quảng bá ổn
định, còn `network_mode: host` làm Compose phụ thuộc host và dễ xung đột cổng
UDP `5353`.

Trên Debian/Ubuntu, cài daemon và công cụ kiểm tra:

```bash
sudo apt update
sudo apt install avahi-daemon avahi-utils
```

Trong section `[server]` của `/etc/avahi/avahi-daemon.conf`, đặt:

```ini
[server]
host-name=smarthome-broker
```

Không tạo section `[server]` thứ hai nếu file đã có section này. Cài file quảng
bá dịch vụ MQTT có sẵn trong repository rồi khởi động lại Avahi:

```bash
sudo install -m 0644 deploy/avahi/smarthome-mqtt.service /etc/avahi/services/smarthome-mqtt.service
sudo systemctl enable --now avahi-daemon
sudo systemctl restart avahi-daemon
```

Kiểm tra từ một máy trong cùng LAN:

```bash
avahi-resolve-host-name smarthome-broker.local
avahi-browse --resolve --terminate _mqtt._tcp
```

Firewall của máy chủ cần cho phép TCP `1883`, TCP cổng web đã chọn và multicast
DNS UDP `5353` trên interface LAN. Backend trong Compose không dùng tên `.local`;
nó kết nối thẳng tới service Docker `broker:1883`.

## Phát triển cục bộ

Các script backend hiện có nằm trong `server/scripts`. Frontend development vẫn
có thể chạy bằng `npm run dev`; giá trị API mặc định dành cho development được
cấu hình trong `client-web/src/config.ts`.

Dynamic Security mặc định tắt trong `appsettings.Development.json`, vì vậy luồng
development hiện tại vẫn có thể dùng broker local không bật plugin. Muốn kiểm
thử đúng phân quyền, hãy chạy stack Compose.
