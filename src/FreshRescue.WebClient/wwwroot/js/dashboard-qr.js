document.addEventListener("DOMContentLoaded", function () {
  const startBtn = document.getElementById("start-camera-btn");
  const stopBtn = document.getElementById("stop-camera-btn");
  const statusSpan = document.getElementById("camera-status");
  const ajaxForm = document.getElementById("ajax-redeem-form");
  const ajaxInput = document.getElementById("ajax-code-input");

  let html5QrCode = null;
  let isScanning = false;

  if (!document.getElementById("reader")) return;

  startBtn.addEventListener("click", async function () {
    if (isScanning) return;

    startBtn.disabled = true;
    statusSpan.textContent = "Đang khởi động camera...";

    try {
      html5QrCode = new Html5Qrcode("reader");
      const qrConfig = { fps: 10, qrbox: { width: 250, height: 250 } };

      await html5QrCode.start(
        { facingMode: "environment" },
        qrConfig,
        async (decodedText) => {
          // Dừng quét tạm thời khi đã đọc được mã
          if (isScanning) {
            isScanning = false;
            try {
              await html5QrCode.stop();
              html5QrCode.clear();
            } catch (e) {
              console.error(e);
            }
            startBtn.style.display = "inline-block";
            stopBtn.style.display = "none";
            statusSpan.textContent = "Đã quét thành công mã: " + decodedText;

            // Submit lên server qua AJAX hoặc reload trang mang kết quả
            ajaxInput.value = decodedText;
            ajaxForm.submit();
          }
        },
        (errorMessage) => {
          // Lỗi quét khung hình liên tục (bỏ qua, không hiển thị phiền phức)
        }
      );

      isScanning = true;
      startBtn.style.display = "none";
      stopBtn.style.display = "inline-block";
      statusSpan.textContent = "Đang quét... Hướng camera về phía mã QR.";
    } catch (err) {
      console.error("Không thể mở camera:", err);
      statusSpan.textContent = "Lỗi: Không thể truy cập camera (hoặc trình duyệt từ chối quyền).";
      startBtn.disabled = false;
    }
  });

  stopBtn.addEventListener("click", async function () {
    if (!html5QrCode || !isScanning) return;

    try {
      await html5QrCode.stop();
      html5QrCode.clear();
    } catch (e) {
      console.error(e);
    }

    isScanning = false;
    stopBtn.style.display = "none";
    startBtn.style.display = "inline-block";
    startBtn.disabled = false;
    statusSpan.textContent = "Camera đã tắt.";
  });
});
