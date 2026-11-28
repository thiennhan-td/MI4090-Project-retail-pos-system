(() => {
    const form = document.querySelector("#return-form");
    if (!form) {
        return;
    }

    const field = form.querySelector("fieldset");
    const search = document.querySelector("#return-search-form");
    const rows = [...form.querySelectorAll("[data-line]")];
    const get = id => document.getElementById(id);
    const cash = get("cash");
    const bank = get("bank");
    const mode = get("mode");
    const feedback = get("feedback");
    const conflictBox = get("return-conflict");
    const conflictCode = get("return-conflict-code");
    const conflictLookup = get("return-conflict-lookup");
    const retryBtn = get("retry");
    const receiptLink = get("receipt");

    const money = n => Math.round(n || 0).toLocaleString("vi-VN") + " đ";
    let pending = null;
    let draft = form.dataset.draft || null;
    let busy = false;

    function update(source) {
        let total = 0;
        for (const row of rows) {
            const qtyInput = row.querySelector(".qty");
            const stockInput = row.querySelector(".stock");
            const qty = qtyInput.valueAsNumber || 0;
            stockInput.max = String(Math.max(0, qty));
            const amount = Math.max(0, qty) * Number(row.dataset.price);
            row.querySelector(".subtotal").textContent = money(amount);
            total += amount;
        }

        get("total").textContent = money(total);
        cash.readOnly = mode.value !== "mixed";
        bank.readOnly = mode.value !== "mixed";

        if (mode.value === "cash") {
            cash.value = String(total);
            bank.value = "0";
        } else if (mode.value === "bank") {
            bank.value = String(total);
            cash.value = "0";
        } else if (source === bank) {
            cash.value = String(Math.max(0, total - (Number(bank.value) || 0)));
        } else if (source === cash || source === mode || !source) {
            bank.value = String(Math.max(0, total - (Number(cash.value) || 0)));
        } else {
            bank.value = String(Math.max(0, total - (Number(cash.value) || 0)));
        }
    }

    function lock(value) {
        field.disabled = value;
        if (search) {
            for (const control of search.elements) {
                control.disabled = value;
            }
        }
    }

    function showMessage(text, isError) {
        conflictBox.hidden = true;
        feedback.hidden = false;
        feedback.className = "pos-alert " + (isError ? "pos-alert-error" : "pos-alert-success");
        feedback.textContent = text;
    }

    function showConflict(code, lookupUrl) {
        feedback.hidden = true;
        conflictCode.textContent = code || "";
        if (lookupUrl) {
            conflictLookup.href = lookupUrl;
        }
        conflictBox.hidden = false;
    }

    async function send() {
        if (busy || !pending) {
            return;
        }
        busy = true;
        lock(true);
        retryBtn.hidden = true;
        conflictBox.hidden = true;

        try {
            const response = await fetch(form.dataset.save + "?xacNhan=" + pending.confirm, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    RequestVerificationToken: form.elements.__RequestVerificationToken.value
                },
                body: pending.body
            });

            if (response.status >= 500) {
                throw new Error("Server error");
            }

            const data = await response.json();
            const isConfirm = pending.confirm;
            const done = isConfirm && response.ok;
            pending = null;

            if (response.status === 409 || data.conflict) {
                showConflict(data.ma, data.traCuuUrl || (form.dataset.lookup + "?chon=" + encodeURIComponent(data.ma || "")));
                lock(false);
                field.disabled = true;
                return;
            }

            showMessage(data.message || (response.ok ? "Đã xử lý phiếu trả." : "Không thể lưu phiếu trả."), !response.ok);

            if (data.ma) {
                receiptLink.href = data.traCuuUrl || (form.dataset.lookup + "?chon=" + encodeURIComponent(data.ma));
                receiptLink.hidden = false;
                if (response.ok) {
                    draft = isConfirm ? null : data.ma;
                }
            }

            lock(false);
            field.disabled = done;
        } catch {
            showMessage("Chưa nhận được phản hồi từ máy chủ. Bấm \"Thử lại\" để gửi lại đúng giao dịch này.", true);
            retryBtn.hidden = false;
        } finally {
            busy = false;
        }
    }

    form.addEventListener("input", e => update(e.target));
    form.addEventListener("change", e => update(e.target));

    form.addEventListener("submit", e => {
        e.preventDefault();
        if (busy || pending || field.disabled) {
            return;
        }

        if (!get("shift").checked) {
            showMessage("Cần mở ca để nhận trả hàng.", true);
            return;
        }

        const lines = [];
        for (const row of rows) {
            const qtyInput = row.querySelector(".qty");
            const stockInput = row.querySelector(".stock");
            const qty = Number(qtyInput.value);
            const stock = Number(stockInput.value);
            const maxQty = Number(qtyInput.max);

            if (!Number.isInteger(qty) || qty < 0 || qty > maxQty) {
                showMessage("Số lượng trả không hợp lệ hoặc vượt quá số lượng còn được trả.", true);
                return;
            }

            if (qty > 0) {
                if (!Number.isInteger(stock) || stock < 0 || stock > qty) {
                    showMessage("Số lượng nhập lại kho phải từ 0 đến số lượng trả.", true);
                    return;
                }
                lines.push({
                    dong: Number(row.dataset.line),
                    soLuong: qty,
                    nhapLai: stock
                });
            }
        }

        if (!lines.length) {
            showMessage("Chọn ít nhất một dòng hàng trả.", true);
            return;
        }

        const isConfirm = e.submitter?.value === "confirm";
        const requestId = (window.crypto && window.crypto.randomUUID)
            ? window.crypto.randomUUID()
            : String(Date.now()) + Math.random().toString(16).slice(2);

        pending = {
            confirm: isConfirm,
            body: JSON.stringify({
                maHoaDon: form.dataset.invoice,
                maYeuCau: requestId,
                maNhap: draft || null,
                caMo: get("shift").checked,
                dongTra: lines,
                tienMat: Number(cash.value) || 0,
                chuyenKhoan: Number(bank.value) || 0
            })
        };

        send();
    });

    retryBtn.addEventListener("click", send);
    update(null);
})();
