(() => {
    const form = document.querySelector("#return-form");
    const searchForm = document.querySelector("#return-search-form");
    const key = "pos-return:" + searchForm.dataset.session;
    const saved = sessionStorage.getItem(key);
    const restored = saved ? JSON.parse(saved) : null;
    if (restored && (!form || form.dataset.invoice !== JSON.parse(restored.body).maHoaDon)) {
        const url = new URL(location.pathname, location.origin);
        url.searchParams.set("maHoaDon", JSON.parse(restored.body).maHoaDon);
        if (url.href !== location.href) location.replace(url);
        return;
    }
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
    let pending = restored;
    let draft = form.dataset.draft || null;
    let busy = false;

    function update(source) {
        let total = 0;
        for (const row of rows) {
            const qtyInput = row.querySelector(".qty");
            const stockInput = row.querySelector(".stock");
            const qty = qtyInput.valueAsNumber || 0;
            if (!qty) stockInput.value = "0";
            stockInput.disabled = !qty;
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

    function showConflict(code, lookupUrl, message) {
        feedback.hidden = true;
        get("conflict-message").textContent = message || "Phiếu trả đã thay đổi.";
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
                signal: AbortSignal.timeout(20000),
                headers: {
                    "Content-Type": "application/json",
                    RequestVerificationToken: form.elements.__RequestVerificationToken.value
                },
                body: pending.body
            });

            if (response.status >= 500) {
                throw new Error("Server error");
            }

            const data = response.ok ? await response.json() : await response.json().catch(() => ({}));
            const isConfirm = pending.confirm;
            const done = isConfirm && response.ok;
            sessionStorage.removeItem(key);
            pending = null;

            if (response.status === 409 || data.conflict) {
                showConflict(data.ma, data.traCuuUrl || (form.dataset.lookup + "?chon=" + encodeURIComponent(data.ma || "")), data.message);
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
                    if (draft) {
                        const url = new URL(location.href);
                        url.searchParams.set("maNhap", draft);
                        location.replace(url);
                        return;
                    }
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
                phienBan: Number(form.dataset.version),
                caMo: get("shift").checked,
                dongTra: lines,
                tienMat: Number(cash.value) || 0,
                chuyenKhoan: Number(bank.value) || 0
            })
        };

        sessionStorage.setItem(key, JSON.stringify(pending));
        send();
    });

    retryBtn.addEventListener("click", send);
    update(null);
    if (pending) {
        const snapshot = JSON.parse(pending.body);
        for (const row of rows) {
            const line = snapshot.dongTra.find(d => d.dong === Number(row.dataset.line));
            row.querySelector(".qty").value = line?.soLuong ?? 0;
            row.querySelector(".stock").value = line?.nhapLai ?? 0;
        }
        mode.value = snapshot.tienMat && snapshot.chuyenKhoan ? "mixed" : snapshot.tienMat ? "cash" : "bank";
        cash.value = snapshot.tienMat;
        update(null);
        lock(true);
        showMessage("Giao dịch đang chờ kết quả. Hãy thử lại để đối chiếu phiếu đã gửi.", true);
        retryBtn.hidden = false;
    }
})();
