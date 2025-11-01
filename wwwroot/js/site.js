// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


document.addEventListener("DOMContentLoaded", () => {
    const modalElement = document.getElementById("featureModal");
    const modalBody = document.getElementById("ModalBody");
    const bsModal = new bootstrap.Modal(modalElement);

    // Khi bấm nút "Thêm mới"
    document.querySelectorAll("[data-feature]").forEach(btn => {
        btn.addEventListener("click", async () => {
            const feature = btn.dataset.feature;
            const action = btn.dataset.action;

            switch (action) {
                case 'add':
                    // Gọi đến Controller lấy dữ liệu động ( xem chi tiết trong Partial Controller hàm GetAddForm )
                    const response = await fetch(`/Partial/GetAddForm?feature=${feature}`);
                    const html = await response.text();
                    modalBody.innerHTML = html;

                    bsModal.show();

                    // Kích hoạt validation + ajax submit
                    bindAjaxForm();

                    break;
                case 'delete':
                    console.log('this is delete action');
                    break;
                case 'update':
                    break;
                case 'refresh':
                    break;
            }
        });
    });

    function bindAjaxForm() {
        const form = modalBody.querySelector("form");
        if (!form) return;

        form.addEventListener("submit", async (e) => {
            e.preventDefault();

            const formData = new FormData(form);
            const response = await fetch(form.action, {
                method: "POST",
                body: formData
            });

            const contentType = response.headers.get("content-type");

            if (contentType && contentType.includes("text/html")) {
                // Lỗi validation => load lại form có lỗi
                modalBody.innerHTML = await response.text();
                bindAjaxForm(); // Gắn lại sự kiện submit
            } else {
                const result = await response.json();
                if (result.success) {
                    bsModal.hide();
                    alert(result.message);
                    location.reload(); // Hoặc cập nhật bảng bằng JS
                }
            }
        });
    }
});

