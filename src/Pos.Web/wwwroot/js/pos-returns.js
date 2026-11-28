(() => {
    const get = id => document.getElementById(id);
    const writePanel = get("return-write");
    if (!writePanel?.dataset.actor) return;

    const form = document.querySelector("#return-form");
    const searchForm = get("return-search-form");
    const field = form?.querySelector("fieldset");
    const rows = [...(form?.querySelectorAll("[data-line]") ?? [])];
    const key = "pos-return:" + writePanel.dataset.session + ":" + writePanel.dataset.actor;
    const cash = get("cash");
    const bank = get("bank");
    const mode = get("mode");
    const feedback = get("feedback");
    const conflictBox = get("return-conflict");
    const conflictCode = get("return-conflict-code");
    const conflictLookup = get("return-conflict-lookup");
    const recoveryBox = get("return-recovery");
    const memoryOnly = get("return-memory-only");
    const retryBtn = get("retry");
    const discardBtn = get("discard-return-request");
    const receiptBox = get("return-receipt");
    const receiptLink = get("receipt");
    const refreshLink = get("return-refresh");
    const requestToken = writePanel.querySelector('input[name="__RequestVerificationToken"]').value;

    const money = n => Math.round(n || 0).toLocaleString("vi-VN") + " đ";
    let busy = false;
    let pending = readPending();
    let savedInTab = Boolean(pending);

    function readPending() {
        let saved;
        try {
            saved = sessionStorage.getItem(key);
        } catch {
            return null;
        }
        if (!saved) return null;

        try {
            const request = JSON.parse(saved);
            const snapshot = JSON.parse(request.body);
            if (typeof request.confirm !== "boolean" || typeof snapshot.maHoaDon !== "string"
                || typeof snapshot.maYeuCau !== "string" || !Array.isArray(snapshot.dongTra)) {
                throw new Error("Malformed snapshot");
            }
            return { ...request, snapshot };
        } catch {
            rememberPending(null);
            showMessage("Bản lưu giao dịch không đọc được. Hãy tra cứu phiếu trả trước khi ghi lại.", true);
            return null;
        }
    }

    function rememberPending(request) {
        try {
            if (request) {
                sessionStorage.setItem(key, JSON.stringify({ confirm: request.confirm, body: request.body }));
            } else {
                sessionStorage.removeItem(key);
            }
            return true;
        } catch {
            return false;
        }
    }

    function updateRecovery() {
        recoveryBox.hidden = !pending;
        const keepThisTab = Boolean(pending) && !savedInTab;
        memoryOnly.hidden = !keepThisTab;
        searchForm.target = keepThisTab ? "_blank" : "_self";
        retryBtn.disabled = busy;
        discardBtn.disabled = busy;
        if (field) field.disabled = Boolean(pending);
    }

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
        } else {
            bank.value = String(Math.max(0, total - (Number(cash.value) || 0)));
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
        conflictCode.textContent = code ? " · Phiếu " + code : "";
        conflictLookup.href = lookupUrl || writePanel.dataset.lookup;
        conflictBox.hidden = false;
        conflictBox.focus();
    }

    async function send() {
        if (busy || !pending) {
            return;
        }
        const request = pending;
        const wasRecovering = recoveryBox.contains(document.activeElement);
        busy = true;
        updateRecovery();
        conflictBox.hidden = true;
        receiptBox.hidden = true;
        refreshLink.hidden = true;
        const sameInvoice = form?.dataset.invoice.toUpperCase() === request.snapshot.maHoaDon.toUpperCase();
        const sameDraft = (form?.dataset.draft || null) === (request.snapshot.maNhap || null);
        const sameVersion = Number(form?.dataset.version) === request.snapshot.phienBan;
        let readOnly = false;

        try {
            const response = await fetch(writePanel.dataset.save + "?xacNhan=" + request.confirm, {
                method: "POST",
                signal: AbortSignal.timeout(20000),
                headers: {
                    "Content-Type": "application/json",
                    RequestVerificationToken: requestToken
                },
                body: request.body
            });

            if (response.status >= 500) {
                throw new Error("Server error");
            }

            const data = await response.json().catch(() => null);
            if (response.status === 401 || response.status === 403) {
                const message = data?.message || "Không có quyền đối chiếu phiếu trả trong phiên hiện tại.";
                showMessage(message + " Yêu cầu chờ vẫn được giữ lại.", true);
                return;
            }
            if (!data?.message) throw new Error("Missing result");
            const isConfirm = request.confirm;
            rememberPending(null);
            pending = null;

            if (response.status === 409 || data.conflict) {
                showConflict(data.ma, data.traCuuUrl, data.message);
                readOnly = sameInvoice && sameDraft && sameVersion;
                return;
            }

            showMessage(data.message, !response.ok);

            if (data.ma) {
                receiptLink.href = data.traCuuUrl || (writePanel.dataset.lookup + "?chon=" + encodeURIComponent(data.ma));
                receiptBox.hidden = false;
                if (response.ok && !isConfirm && sameInvoice && sameDraft && sameVersion) {
                    const url = new URL(location.href);
                    url.searchParams.set("maNhap", data.ma);
                    location.replace(url);
                    return;
                }
            }
            readOnly = response.ok && isConfirm && sameInvoice;
            if (readOnly) {
                const url = new URL(location.href);
                if (sameDraft) url.searchParams.delete("maNhap");
                refreshLink.href = url.href;
                refreshLink.hidden = false;
            }
        } catch {
            if (pending) {
                showMessage(`Chưa xác định kết quả cho hóa đơn ${request.snapshot.maHoaDon}. Bấm “Thử lại” để đối chiếu đúng giao dịch này.`, true);
            }
        } finally {
            busy = false;
            updateRecovery();
            if (readOnly && field) field.disabled = true;
            if (wasRecovering && !feedback.hidden) feedback.focus();
        }
    }

    retryBtn.addEventListener("click", send);
    window.addEventListener("beforeunload", event => {
        if (pending && !savedInTab) event.preventDefault();
    });
    discardBtn.addEventListener("click", () => {
        if (!pending || busy || !window.confirm("Máy chủ có thể đã ghi phiếu. Chỉ bỏ yêu cầu chờ sau khi đã kiểm tra tra cứu phiếu trả. Bạn muốn tiếp tục?")) return;
        rememberPending(null);
        pending = null;
        updateRecovery();
        showMessage("Đã bỏ yêu cầu chờ trên tab này. Hãy kiểm tra phiếu đã ghi trước khi lập lại.", false);
        feedback.focus();
    });

    if (pending) {
        showMessage(`Hóa đơn ${pending.snapshot.maHoaDon} đang có yêu cầu ghi ${pending.confirm ? "xác nhận" : "nháp"} chờ kết quả. Bạn vẫn có thể tìm hóa đơn khác.`, true);
    }
    updateRecovery();
    if (!form) return;

    form.addEventListener("input", e => update(e.target));
    form.addEventListener("change", e => update(e.target));

    form.addEventListener("submit", e => {
        e.preventDefault();
        if (busy || pending || field.disabled) {
            return;
        }

        if (!form.dataset.shift) {
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

        const snapshot = {
            maHoaDon: form.dataset.invoice,
            maYeuCau: requestId,
            maNhap: form.dataset.draft || null,
            phienBan: Number(form.dataset.version),
            caMo: true,
            maCa: form.dataset.shift,
            dongTra: lines,
            tienMat: Number(cash.value) || 0,
            chuyenKhoan: Number(bank.value) || 0
        };
        pending = { confirm: isConfirm, snapshot, body: JSON.stringify(snapshot) };
        savedInTab = rememberPending(pending);
        send();
    });

    update(null);
})();
