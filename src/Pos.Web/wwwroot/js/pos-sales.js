(function () {
    const app = document.getElementById("pos-sales-app");
    if (!app) {
        return;
    }

    const confirmUrl = app.dataset.confirmUrl;
    const saveUrl = app.dataset.saveUrl;
    const cartJumpLink = document.getElementById("pos-sales-jump");
    const requestToken = app.querySelector('input[name="__RequestVerificationToken"]').value;
    const savedDraftElement = document.getElementById("pos-saved-draft");
    let savedDraft = savedDraftElement ? JSON.parse(savedDraftElement.textContent) : null;
    const recoveryCode = app.hidden ? new URLSearchParams(window.location.search).get("maNhap")?.trim() : null;
    let pendingRequest = readPendingRequest(savedDraft?.maHoaDon ?? recoveryCode);
    if (app.hidden) {
        if (!pendingRequest) {
            return;
        }
        document.getElementById("pos-draft-unavailable").hidden = true;
        app.hidden = false;
        cartJumpLink.hidden = false;
    }
    let savedState = null;
    let currentRequestId = app.dataset.requestId;
    let isSubmitting = false;
    let isConfirmed = false;
    let hasConflict = false;
    let activeCategory = "all";
    let paymentMode = "TienMat";
    let lastShiftOpenState = Boolean(app.dataset.currentShift);

    const cart = new Map();
    const productButtons = Array.from(document.querySelectorAll(".pos-product-card"));
    const categoryButtons = Array.from(document.querySelectorAll(".pos-cat-btn"));
    const modeButtons = Array.from(document.querySelectorAll(".pos-mode-btn"));
    const denomButtons = Array.from(document.querySelectorAll(".pos-denom-btn"));

    const searchInput = document.getElementById("pos-product-search");
    const emptyCatalog = document.getElementById("pos-product-empty");
    const cartList = document.getElementById("pos-cart-list");
    const cartError = document.getElementById("pos-cart-error");
    const draftStatus = document.getElementById("pos-draft-status");
    const grandTotalEl = document.getElementById("pos-grand-total");
    const allocatedTotalEl = document.getElementById("pos-allocated-total");
    const remainingTotalEl = document.getElementById("pos-remaining-total");
    const changeTotalEl = document.getElementById("pos-change-total");
    const cashAllocSummaryEl = document.getElementById("pos-cash-alloc-summary");

    const cashAllocGroup = document.getElementById("pos-cash-alloc-group");
    const cashGivenGroup = document.getElementById("pos-cash-given-group");
    const transferGroup = document.getElementById("pos-transfer-group");

    const cashAllocInput = document.getElementById("pos-cash-allocated");
    const cashGivenInput = document.getElementById("pos-cash-given");
    const transferInput = document.getElementById("pos-transfer-amount");

    const cashGivenError = document.getElementById("pos-cash-given-error");
    const paymentError = document.getElementById("pos-payment-error");
    const saveActionError = document.getElementById("pos-save-action-error");
    const actionError = document.getElementById("pos-action-error");

    const fillCashBtn = document.getElementById("pos-fill-cash-remaining");
    const fillTransferBtn = document.getElementById("pos-fill-transfer-remaining");
    const confirmBtn = document.getElementById("pos-btn-confirm");
    const saveBtn = document.getElementById("pos-btn-save-draft");
    const newInvoiceBtn = document.getElementById("pos-btn-new-invoice");
    const cancelDraftLink = document.getElementById("pos-link-cancel-draft");
    const shiftToggle = document.getElementById("pos-shift-open-toggle");
    const shiftBadge = document.getElementById("pos-shift-badge");

    const confirmResult = document.getElementById("pos-confirm-result");
    const conflictResult = document.getElementById("pos-conflict-result");
    const conflictMessage = document.getElementById("pos-conflict-message");
    const conflictEditLink = document.getElementById("pos-conflict-edit");
    const conflictLookupLink = document.getElementById("pos-conflict-lookup");
    const confirmedCodeEl = document.getElementById("pos-confirmed-code");
    const confirmedDetailEl = document.getElementById("pos-confirmed-detail");
    const lookupLink = document.getElementById("pos-link-lookup");
    const printLink = document.getElementById("pos-link-print");

    const moneyFmt = new Intl.NumberFormat("vi-VN");
    function formatMoney(amount) {
        return moneyFmt.format(Math.round(amount || 0)) + " đ";
    }

    function isLocked() {
        return isSubmitting || isConfirmed || hasConflict || pendingRequest !== null;
    }

    function parseMoneyInput(input) {
        if (input.validity.badInput) {
            return { valid: false, empty: false, value: 0 };
        }
        const raw = (input.value || "").trim();
        if (raw === "") {
            return { valid: true, empty: true, value: 0 };
        }
        const num = Number(raw);
        if (!Number.isSafeInteger(num) || num < 0) {
            return { valid: false, empty: false, value: 0 };
        }
        return { valid: true, empty: false, value: num };
    }

    function getGrandTotal() {
        let sum = 0;
        for (const line of cart.values()) {
            if (line.qty > 0) {
                sum += line.qty * line.price;
            }
        }
        return sum;
    }

    function setError(el, message) {
        if (!el) {
            return;
        }
        if (message) {
            el.textContent = message;
            el.hidden = false;
        } else {
            el.textContent = "";
            el.hidden = true;
        }
    }

    function clearAllErrors() {
        setError(cartError, "");
        setError(cashGivenError, "");
        setError(paymentError, "");
        setError(saveActionError, "");
        setError(actionError, "");
    }

    function getWriteError(confirm) {
        return confirm ? actionError : saveActionError;
    }

    function getEditorState() {
        const inputs = paymentMode === "HonHop" ? [cashAllocInput, cashGivenInput, transferInput]
            : paymentMode === "TienMat" ? [cashGivenInput] : [];
        return JSON.stringify([
            paymentMode, inputs.map((input) => [input.value, input.validity.badInput]),
            Array.from(cart.values(), (line) => [line.code, line.rawQty, line.invalid])
        ]);
    }

    function hasUnsavedChanges() {
        return !isConfirmed && savedState !== null && getEditorState() !== savedState;
    }

    function updateDraftStatus() {
        if (isSubmitting) {
            draftStatus.textContent = pendingRequest.confirm ? "Đang xác nhận hóa đơn…" : "Đang lưu hóa đơn nháp…";
        } else if (pendingRequest) {
            draftStatus.textContent = "Chờ thử lại · Giỏ hàng đang được khóa";
        } else if (isConfirmed) {
            draftStatus.textContent = `Đã xác nhận · ${confirmedCodeEl.textContent}`;
        } else if (hasConflict) {
            draftStatus.textContent = "Hóa đơn đã thay đổi · Mở phiên bản hiện tại để tiếp tục";
        } else if (savedDraft) {
            const status = hasUnsavedChanges() ? "Có thay đổi chưa lưu" : "Đã lưu nháp";
            draftStatus.textContent = `${status} · ${savedDraft.maHoaDon}`;
        } else {
            draftStatus.textContent = hasUnsavedChanges() ? "Bản nháp chưa lưu" : "Hóa đơn mới";
        }
    }

    function syncControlLocks() {
        const locked = isLocked();

        for (const btn of productButtons) {
            btn.disabled = locked;
        }
        for (const ctrl of cartList.querySelectorAll("button, input")) {
            ctrl.disabled = locked;
        }
        for (const btn of modeButtons) {
            btn.disabled = locked;
        }
        for (const btn of denomButtons) {
            btn.disabled = locked;
        }

        cashAllocInput.disabled = locked;
        cashGivenInput.disabled = locked;
        transferInput.disabled = locked;
        fillCashBtn.disabled = locked;
        fillTransferBtn.disabled = locked;
        shiftToggle.disabled = locked || !app.dataset.currentShift;

        const blockWrite = isSubmitting || isConfirmed || hasConflict;
        const saving = pendingRequest?.confirm === false;
        const confirming = pendingRequest?.confirm === true;
        newInvoiceBtn.disabled = isSubmitting || pendingRequest !== null;
        saveBtn.disabled = blockWrite || confirming;
        confirmBtn.disabled = blockWrite || saving;
        saveBtn.textContent = saving ? (isSubmitting ? "Đang lưu…" : "Thử lưu lại") : "Lưu nháp";
        confirmBtn.textContent = confirming ? (isSubmitting ? "Đang xác nhận…" : "Thử lại") : "Xác nhận bán hàng";
        if (isConfirmed) {
            confirmBtn.textContent = `Đã xác nhận (${confirmedCodeEl.textContent})`;
        }
        cancelDraftLink.hidden = !savedDraft || locked;
        updateDraftStatus();
    }

    function filterProducts() {
        const keyword = (searchInput.value || "").trim().toLocaleLowerCase("vi");
        let visibleCount = 0;
        const locked = isLocked();
        for (const btn of productButtons) {
            const matchCat = activeCategory === "all" || btn.dataset.nhom === activeCategory;
            const hay = `${btn.dataset.maHang} ${btn.dataset.tenHang}`.toLocaleLowerCase("vi");
            const matchText = hay.includes(keyword);
            const show = matchCat && matchText;
            btn.hidden = !show;
            btn.disabled = locked;
            if (show) {
                visibleCount++;
            }
        }
        emptyCatalog.hidden = visibleCount > 0;
        for (const btn of categoryButtons) {
            const active = btn.dataset.category === activeCategory;
            btn.classList.toggle("is-active", active);
            btn.setAttribute("aria-pressed", String(active));
        }
    }

    function getPaymentNumbers() {
        const due = getGrandTotal();
        if (paymentMode === "TienMat") {
            const cashGiven = parseMoneyInput(cashGivenInput);
            return {
                due,
                cashAlloc: due,
                cashGiven,
                transfer: { valid: true, empty: true, value: 0 },
                cashAllocParsed: { valid: true, empty: false, value: due }
            };
        }
        if (paymentMode === "ChuyenKhoan") {
            return {
                due,
                cashAlloc: 0,
                cashGiven: { valid: true, empty: true, value: 0 },
                transfer: { valid: true, empty: false, value: due },
                cashAllocParsed: { valid: true, empty: true, value: 0 }
            };
        }
        const cashAllocParsed = parseMoneyInput(cashAllocInput);
        const cashGiven = parseMoneyInput(cashGivenInput);
        const transfer = parseMoneyInput(transferInput);
        return {
            due,
            cashAlloc: cashAllocParsed.valid ? cashAllocParsed.value : 0,
            cashGiven,
            transfer,
            cashAllocParsed
        };
    }

    function updateTotalsOnly() {
        const p = getPaymentNumbers();
        const allocated = p.cashAlloc + (p.transfer.valid ? p.transfer.value : 0);
        const remaining = Math.max(0, p.due - allocated);
        const change = p.cashAlloc > 0 && p.cashGiven.valid && p.cashGiven.value > p.cashAlloc
            ? p.cashGiven.value - p.cashAlloc
            : 0;

        grandTotalEl.textContent = formatMoney(p.due);
        cashAllocSummaryEl.textContent = formatMoney(p.cashAlloc);
        allocatedTotalEl.textContent = formatMoney(allocated);
        remainingTotalEl.textContent = formatMoney(remaining);
        changeTotalEl.textContent = formatMoney(change);

        if (paymentMode === "ChuyenKhoan") {
            transferInput.value = p.due > 0 ? String(p.due) : "";
        }

        if (p.cashAlloc > 0 && !p.cashGiven.empty && p.cashGiven.valid && p.cashGiven.value < p.cashAlloc) {
            setError(cashGivenError, "Tiền khách đưa đang nhỏ hơn tiền mặt phân bổ (" + formatMoney(p.cashAlloc) + ").");
        } else if (!p.cashGiven.valid) {
            setError(cashGivenError, "Tiền khách đưa phải là số nguyên không âm.");
        } else {
            setError(cashGivenError, "");
        }
        updateDraftStatus();
    }

    function applyModeUI(suggestCashGiven = true) {
        for (const btn of modeButtons) {
            const active = btn.dataset.mode === paymentMode;
            btn.classList.toggle("is-active", active);
            btn.setAttribute("aria-pressed", String(active));
        }
        cashAllocGroup.hidden = paymentMode !== "HonHop";
        cashGivenGroup.hidden = paymentMode === "ChuyenKhoan";
        transferGroup.hidden = paymentMode === "TienMat";
        transferInput.readOnly = paymentMode === "ChuyenKhoan";
        fillTransferBtn.hidden = paymentMode !== "HonHop";

        const due = getGrandTotal();
        if (paymentMode === "ChuyenKhoan") {
            transferInput.value = due > 0 ? String(due) : "";
        } else if (paymentMode === "TienMat" && suggestCashGiven) {
            if (due > 0 && !cashGivenInput.value) {
                cashGivenInput.value = String(due);
            }
        }
        clearAllErrors();
        updateTotalsOnly();
    }

    function renderCart() {
        cartList.replaceChildren();
        const locked = isLocked();
        if (cart.size === 0) {
            const p = document.createElement("p");
            p.className = "pos-muted pos-empty-cart";
            p.textContent = "Chưa có mặt hàng trong giỏ. Chọn mặt hàng bên trái để thêm.";
            cartList.appendChild(p);
        } else {
            for (const line of cart.values()) {
                const row = document.createElement("div");
                row.className = "pos-cart-row";
                row.dataset.maHang = line.code;

                const info = document.createElement("div");
                info.className = "pos-cart-info";
                const nameEl = document.createElement("strong");
                nameEl.textContent = line.name;
                const unitEl = document.createElement("span");
                unitEl.className = "pos-muted pos-num";
                unitEl.style.fontSize = "0.8rem";
                unitEl.textContent = `${formatMoney(line.price)} / ${line.unit}`;
                info.append(nameEl, unitEl);

                const controls = document.createElement("div");
                controls.className = "pos-cart-qty-wrap";

                const decBtn = document.createElement("button");
                decBtn.type = "button";
                decBtn.className = "pos-qty-btn";
                decBtn.textContent = "−";
                decBtn.setAttribute("aria-label", "Giảm số lượng " + line.name);
                decBtn.disabled = locked;
                decBtn.addEventListener("click", () => {
                    if (isLocked()) {
                        return;
                    }
                    updateLineQuantity(line.code, line.qty - 1, true);
                });

                const qtyInput = document.createElement("input");
                qtyInput.type = "number";
                qtyInput.className = "pos-input pos-qty-input";
                qtyInput.min = "1";
                qtyInput.max = String(line.stock);
                qtyInput.step = "1";
                qtyInput.inputMode = "numeric";
                qtyInput.value = line.rawQty;
                qtyInput.disabled = locked;
                qtyInput.setAttribute("aria-label", "Số lượng " + line.name);
                qtyInput.addEventListener("input", () => {
                    if (isLocked()) {
                        qtyInput.value = line.rawQty;
                        return;
                    }
                    line.rawQty = qtyInput.value;
                    const parsed = Number(qtyInput.value);
                    if (!qtyInput.value.trim() || !Number.isInteger(parsed) || parsed <= 0) {
                        line.qty = 0;
                        line.invalid = true;
                        setError(cartError, `Số lượng của ${line.name} phải là số nguyên lớn hơn 0.`);
                    } else if (parsed > line.stock) {
                        line.qty = parsed;
                        line.invalid = true;
                        setError(cartError, `Số lượng của ${line.name} vượt tồn mẫu hiện có (${line.stock} ${line.unit}).`);
                    } else {
                        line.qty = parsed;
                        line.invalid = false;
                        setError(cartError, "");
                    }
                    subtotalEl.textContent = formatMoney(line.qty > 0 ? line.qty * line.price : 0);
                    updateTotalsOnly();
                });

                const incBtn = document.createElement("button");
                incBtn.type = "button";
                incBtn.className = "pos-qty-btn";
                incBtn.textContent = "+";
                incBtn.setAttribute("aria-label", "Tăng số lượng " + line.name);
                incBtn.disabled = locked;
                incBtn.addEventListener("click", () => {
                    if (isLocked()) {
                        return;
                    }
                    updateLineQuantity(line.code, line.qty + 1, true);
                });

                const delBtn = document.createElement("button");
                delBtn.type = "button";
                delBtn.className = "pos-qty-del";
                delBtn.textContent = "Xóa";
                delBtn.setAttribute("aria-label", "Xóa " + line.name);
                delBtn.disabled = locked;
                delBtn.addEventListener("click", () => {
                    if (isLocked()) {
                        return;
                    }
                    cart.delete(line.code);
                    clearAllErrors();
                    renderCart();
                });

                const subtotalEl = document.createElement("strong");
                subtotalEl.className = "pos-num pos-cart-subtotal";
                subtotalEl.textContent = formatMoney(line.qty > 0 ? line.qty * line.price : 0);

                controls.append(decBtn, qtyInput, incBtn, delBtn);
                row.append(info, controls, subtotalEl);
                cartList.appendChild(row);
            }
        }
        updateTotalsOnly();
        syncControlLocks();
    }

    function updateLineQuantity(code, nextQty, rerender) {
        if (isLocked()) {
            return;
        }
        const line = cart.get(code);
        if (!line) {
            return;
        }
        if (nextQty <= 0) {
            cart.delete(code);
            clearAllErrors();
            if (rerender) {
                renderCart();
            }
            return;
        }
        if (nextQty > line.stock) {
            setError(cartError, `Số lượng của ${line.name} vượt tồn mẫu hiện có (${line.stock} ${line.unit}).`);
            return;
        }
        line.qty = nextQty;
        line.rawQty = String(nextQty);
        line.invalid = false;
        setError(cartError, "");
        if (rerender) {
            renderCart();
        }
    }

    function addProduct(btn) {
        if (isLocked()) {
            return;
        }
        const code = btn.dataset.maHang;
        const existing = cart.get(code);
        if (existing) {
            updateLineQuantity(code, existing.qty + 1, true);
            return;
        }
        setCartLine(btn, 1);
        setError(cartError, "");
        renderCart();
    }

    function setCartLine(btn, quantity) {
        cart.set(btn.dataset.maHang, {
            code: btn.dataset.maHang,
            name: btn.dataset.tenHang,
            unit: btn.dataset.dvt,
            price: Number(btn.dataset.gia),
            stock: Number(btn.dataset.ton),
            qty: quantity,
            rawQty: String(quantity),
            invalid: false
        });
    }

    function newRequestId() {
        return window.crypto?.randomUUID?.().replaceAll("-", "")
            ?? String(Date.now()) + Math.random().toString(16).slice(2);
    }

    function pendingStorageKey(draftCode) {
        return "retail-pos:f3:pending:" + (draftCode?.toUpperCase() ?? "new");
    }

    function readPendingRequest(draftCode) {
        try {
            const request = JSON.parse(window.sessionStorage.getItem(pendingStorageKey(draftCode)));
            return typeof request?.confirm === "boolean" && Array.isArray(request.payload?.dongBans) ? request : null;
        } catch {
            return null;
        }
    }

    function rememberPendingRequest(request, keep) {
        try {
            const key = pendingStorageKey(request.payload.maNhap);
            if (keep) {
                window.sessionStorage.setItem(key, JSON.stringify(request));
            } else {
                window.sessionStorage.removeItem(key);
            }
        } catch {
            // Keep the current tab's retry available when storage is disabled.
        }
    }

    function resetInvoice() {
        if (isSubmitting || pendingRequest) {
            return;
        }
        if (hasUnsavedChanges() && !window.confirm("Bỏ các thay đổi chưa lưu và lập hóa đơn mới?")) {
            return;
        }

        isConfirmed = false;
        hasConflict = false;
        savedDraft = null;
        currentRequestId = newRequestId();
        cart.clear();
        cashAllocInput.value = "";
        cashGivenInput.value = "";
        transferInput.value = "";
        paymentMode = "TienMat";
        confirmResult.hidden = true;
        conflictResult.hidden = true;
        window.history.replaceState(null, "", app.dataset.newUrl);
        applyModeUI(false);
        savedState = getEditorState();
        renderCart();
        filterProducts();
    }

    function createPayload(confirm) {
        if (!shiftToggle.checked) {
            setError(getWriteError(confirm), "Ca bán hiện tại đã đóng. Vui lòng mở ca trước khi ghi hóa đơn.");
            return null;
        }
        if (cart.size === 0) {
            setError(cartError, "Giỏ hàng đang trống. Vui lòng chọn ít nhất một mặt hàng.");
            return null;
        }

        const lines = [];
        for (const line of cart.values()) {
            if (line.invalid || !Number.isInteger(line.qty) || line.qty <= 0) {
                setError(cartError, `Số lượng của ${line.name} không hợp lệ (cần số nguyên dương).`);
                return null;
            }
            if (line.qty > line.stock) {
                setError(cartError, `Số lượng của ${line.name} vượt tồn mẫu hiện có (${line.stock} ${line.unit}).`);
                return null;
            }
            lines.push({ maHang: line.code, soLuong: line.qty });
        }

        const payment = getPaymentNumbers();
        if (!payment.cashAllocParsed.valid || !payment.transfer.valid || !payment.cashGiven.valid) {
            setError(confirm ? paymentError : saveActionError, "Số tiền thanh toán phải là số nguyên VND không âm.");
            return null;
        }
        if (confirm) {
            const allocated = payment.cashAlloc + payment.transfer.value;
            if (allocated !== payment.due) {
                setError(paymentError, `Tổng phân bổ (${formatMoney(allocated)}) phải bằng đúng giá trị hóa đơn (${formatMoney(payment.due)}).`);
                return null;
            }
            if (payment.cashAlloc > 0 && (payment.cashGiven.empty || payment.cashGiven.value < payment.cashAlloc)) {
                setError(cashGivenError, `Tiền mặt khách đưa phải lớn hơn hoặc bằng tiền mặt phân bổ (${formatMoney(payment.cashAlloc)}).`);
                return null;
            }
        }

        return {
            maYeuCau: currentRequestId,
            maNhap: savedDraft?.maHoaDon ?? null,
            phienBan: savedDraft?.phienBan ?? null,
            cheDoThanhToan: paymentMode,
            tienMatPhanBo: payment.cashAllocParsed.empty ? null : payment.cashAlloc,
            tienKhachDua: payment.cashGiven.empty || (confirm && payment.cashAlloc === 0)
                ? null : payment.cashGiven.value,
            tienChuyenKhoan: payment.transfer.empty ? null : payment.transfer.value,
            caDangMo: shiftToggle.checked,
            dongBans: lines
        };
    }

    function showConflict(data) {
        hasConflict = true;
        conflictMessage.textContent = data.message || "Hóa đơn đã thay đổi. Vui lòng xem bản hiện tại.";
        conflictEditLink.hidden = !data.suaUrl;
        if (data.suaUrl) {
            conflictEditLink.href = data.suaUrl;
        }
        conflictLookupLink.href = data.traCuuUrl;
        conflictResult.hidden = false;
        conflictResult.focus();
    }

    function showConfirmed(data) {
        isConfirmed = true;
        confirmedCodeEl.textContent = data.maHoaDon;
        confirmedDetailEl.textContent =
            `Tổng thu: ${formatMoney(data.tongTien)} (Tiền mặt: ${formatMoney(data.tienMatPhanBo)}, Chuyển khoản: ${formatMoney(data.tienChuyenKhoan)}) · Tiền thừa: ${formatMoney(data.tienThua)}`;
        lookupLink.href = data.traCuuUrl;
        printLink.href = data.inUrl;
        confirmResult.hidden = false;
    }

    function setSavedDraft(data) {
        savedDraft = { maHoaDon: data.maHoaDon, phienBan: data.phienBan };
        savedState = getEditorState();
        currentRequestId = newRequestId();
        cancelDraftLink.href = data.traCuuUrl + "#huy-hoa-don";
        window.history.replaceState(null, "", data.suaUrl);
    }

    function showRetryMessage() {
        const action = pendingRequest.confirm ? "Thử lại" : "Thử lưu lại";
        setError(getWriteError(pendingRequest.confirm), `Chưa nhận được kết quả từ máy chủ. Bấm “${action}” để gửi lại đúng giao dịch này.`);
    }

    async function writeInvoice(confirm) {
        if (isSubmitting || isConfirmed || hasConflict || (pendingRequest && pendingRequest.confirm !== confirm)) {
            return;
        }

        clearAllErrors();
        if (!pendingRequest) {
            const payload = createPayload(confirm);
            if (!payload) {
                return;
            }
            pendingRequest = { confirm, payload };
            rememberPendingRequest(pendingRequest, true);
        }

        isSubmitting = true;
        syncControlLocks();
        const controller = new AbortController();
        const timeout = window.setTimeout(() => controller.abort(), 15000);

        try {
            const res = await fetch(confirm ? confirmUrl : saveUrl, {
                method: "POST",
                headers: { "Content-Type": "application/json", RequestVerificationToken: requestToken },
                body: JSON.stringify(pendingRequest.payload),
                signal: controller.signal
            });
            if (res.status >= 500) {
                throw new Error("Server error " + res.status);
            }

            const data = res.headers.get("content-type")?.includes("json") ? await res.json().catch(() => null) : null;
            if (res.status === 409 && data?.traCuuUrl) {
                showConflict(data);
            } else if (!res.ok || !data?.ok) {
                if (res.ok) {
                    throw new Error("Missing invoice response");
                }
                const errors = { cart: cartError, "cash-given": cashGivenError, payment: paymentError };
                const message = data?.message || "Không thể ghi hóa đơn. Vui lòng kiểm tra dữ liệu hoặc mở lại màn bán hàng.";
                if (recoveryCode) {
                    showConflict({ message, traCuuUrl: document.getElementById("pos-unavailable-lookup").href });
                } else {
                    const errorElement = confirm || data?.field === "cart" ? errors[data?.field] : null;
                    setError(errorElement || getWriteError(confirm), message);
                }
            } else if (confirm) {
                showConfirmed(data);
            } else {
                setSavedDraft(data);
            }

            rememberPendingRequest(pendingRequest, false);
            pendingRequest = null;
        } catch {
            showRetryMessage();
        } finally {
            window.clearTimeout(timeout);
            isSubmitting = false;
            syncControlLocks();
        }
    }

    // Bind events
    for (const btn of productButtons) {
        btn.addEventListener("click", () => addProduct(btn));
    }

    for (const btn of categoryButtons) {
        btn.addEventListener("click", () => {
            activeCategory = btn.dataset.category;
            filterProducts();
        });
    }

    searchInput.addEventListener("input", filterProducts);

    cartJumpLink.addEventListener("click", (event) => {
        if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
            return;
        }
        event.preventDefault();
        const editor = document.getElementById("pos-invoice-editor");
        editor.focus({ preventScroll: true });
        editor.scrollIntoView({ block: "start" });
    });

    for (const btn of modeButtons) {
        btn.addEventListener("click", () => {
            if (isLocked()) {
                return;
            }
            paymentMode = btn.dataset.mode;
            applyModeUI();
        });
    }

    cashAllocInput.addEventListener("input", () => {
        if (isLocked()) {
            return;
        }
        setError(saveActionError, "");
        if (paymentMode === "HonHop") {
            const parsed = parseMoneyInput(cashAllocInput);
            const due = getGrandTotal();
            if (parsed.valid && parsed.value <= due) {
                transferInput.value = String(due - parsed.value);
            }
        }
        updateTotalsOnly();
    });

    transferInput.addEventListener("input", () => {
        if (isLocked()) {
            return;
        }
        setError(saveActionError, "");
        if (paymentMode === "HonHop") {
            const parsed = parseMoneyInput(transferInput);
            const due = getGrandTotal();
            if (parsed.valid && parsed.value <= due) {
                cashAllocInput.value = String(due - parsed.value);
            }
        }
        updateTotalsOnly();
    });

    cashGivenInput.addEventListener("input", () => {
        if (isLocked()) {
            return;
        }
        setError(saveActionError, "");
        updateTotalsOnly();
    });

    fillCashBtn.addEventListener("click", () => {
        if (isLocked()) {
            return;
        }
        const due = getGrandTotal();
        const tr = parseMoneyInput(transferInput);
        const rem = Math.max(0, due - (tr.valid ? tr.value : 0));
        cashAllocInput.value = String(rem);
        updateTotalsOnly();
    });

    fillTransferBtn.addEventListener("click", () => {
        if (isLocked()) {
            return;
        }
        const due = getGrandTotal();
        const ca = parseMoneyInput(cashAllocInput);
        const rem = Math.max(0, due - (ca.valid ? ca.value : 0));
        transferInput.value = String(rem);
        updateTotalsOnly();
    });

    for (const btn of denomButtons) {
        btn.addEventListener("click", () => {
            if (isLocked()) {
                return;
            }
            const p = getPaymentNumbers();
            const val = btn.dataset.denom === "exact" ? p.cashAlloc : Number(btn.dataset.denom);
            cashGivenInput.value = String(val);
            updateTotalsOnly();
        });
    }

    shiftToggle.addEventListener("change", () => {
        if (isLocked()) {
            shiftToggle.checked = lastShiftOpenState;
            return;
        }
        lastShiftOpenState = shiftToggle.checked;
        if (shiftToggle.checked) {
            shiftBadge.textContent = app.dataset.currentShift
                ? "Ca đang mở · " + app.dataset.currentShift
                : "Chưa mở ca";
            shiftBadge.classList.add("pos-pill-teal");
            setError(paymentError, "");
            setError(saveActionError, "");
            setError(actionError, "");
        } else {
            shiftBadge.textContent = "Ca đã đóng (giả lập)";
            shiftBadge.classList.remove("pos-pill-teal");
        }
    });

    confirmBtn.addEventListener("click", () => writeInvoice(true));
    saveBtn.addEventListener("click", () => writeInvoice(false));
    newInvoiceBtn.addEventListener("click", resetInvoice);

    window.addEventListener("beforeunload", (event) => {
        if (hasUnsavedChanges() || pendingRequest) {
            event.preventDefault();
            event.returnValue = true;
        }
    });

    function restoreInvoice(lines, payment) {
        cart.clear();
        for (const line of lines) {
            const product = productButtons.find((button) => button.dataset.maHang === line.maHang);
            if (product) {
                setCartLine(product, line.soLuong ?? line.soLuongBan);
            }
        }
        if (payment) {
            paymentMode = payment.cheDoThanhToan;
            cashGivenInput.value = payment.tienKhachDua ?? "";
            cashAllocInput.value = paymentMode === "HonHop" ? payment.tienMatPhanBo ?? "" : "";
            transferInput.value = paymentMode === "HonHop" ? payment.tienChuyenKhoan ?? "" : "";
        }
    }

    if (savedDraft) {
        restoreInvoice(savedDraft.dongBans, savedDraft.thanhToanNhap);
    }
    applyModeUI(false);
    savedState = getEditorState();
    if (pendingRequest) {
        const { payload } = pendingRequest;
        currentRequestId = payload.maYeuCau;
        restoreInvoice(payload.dongBans, payload);
        applyModeUI(false);
        showRetryMessage();
    }
    renderCart();
    filterProducts();
})();
