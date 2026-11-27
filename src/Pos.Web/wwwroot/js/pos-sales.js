(function () {
    const app = document.getElementById("pos-sales-app");
    if (!app) {
        return;
    }

    const confirmUrl = app.dataset.confirmUrl;
    let currentRequestId = app.dataset.requestId;
    let isSubmitting = false;
    let isConfirmed = false;
    let activeCategory = "all";
    let paymentMode = "TienMat";

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

    const fillCashBtn = document.getElementById("pos-fill-cash-remaining");
    const fillTransferBtn = document.getElementById("pos-fill-transfer-remaining");
    const confirmBtn = document.getElementById("pos-btn-confirm");
    const clearCartBtn = document.getElementById("pos-btn-clear-cart");
    const newInvoiceBtn = document.getElementById("pos-btn-new-invoice");
    const shiftToggle = document.getElementById("pos-shift-open-toggle");
    const shiftBadge = document.getElementById("pos-shift-badge");

    const confirmResult = document.getElementById("pos-confirm-result");
    const confirmedCodeEl = document.getElementById("pos-confirmed-code");
    const confirmedDetailEl = document.getElementById("pos-confirmed-detail");
    const lookupLink = document.getElementById("pos-link-lookup");
    const printLink = document.getElementById("pos-link-print");

    const moneyFmt = new Intl.NumberFormat("vi-VN");
    function formatMoney(amount) {
        return moneyFmt.format(Math.round(amount || 0)) + " đ";
    }

    function parseMoneyInput(input) {
        const raw = (input.value || "").trim();
        if (raw === "") {
            return { valid: true, empty: true, value: 0 };
        }
        const num = Number(raw);
        if (!Number.isFinite(num) || !Number.isInteger(num) || num < 0) {
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
    }

    function filterProducts() {
        const keyword = (searchInput.value || "").trim().toLocaleLowerCase("vi");
        let visibleCount = 0;
        for (const btn of productButtons) {
            const matchCat = activeCategory === "all" || btn.dataset.nhom === activeCategory;
            const hay = `${btn.dataset.maHang} ${btn.dataset.tenHang}`.toLocaleLowerCase("vi");
            const matchText = hay.includes(keyword);
            const show = matchCat && matchText;
            btn.hidden = !show;
            btn.disabled = isConfirmed;
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
    }

    function applyModeUI() {
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
        } else if (paymentMode === "TienMat") {
            if (due > 0 && !cashGivenInput.value) {
                cashGivenInput.value = String(due);
            }
        }
        clearAllErrors();
        updateTotalsOnly();
    }

    function renderCart() {
        cartList.replaceChildren();
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
                decBtn.disabled = isConfirmed;
                decBtn.addEventListener("click", () => updateLineQuantity(line.code, line.qty - 1, true));

                const qtyInput = document.createElement("input");
                qtyInput.type = "number";
                qtyInput.className = "pos-input pos-qty-input";
                qtyInput.min = "1";
                qtyInput.max = String(line.stock);
                qtyInput.step = "1";
                qtyInput.inputMode = "numeric";
                qtyInput.value = line.rawQty;
                qtyInput.disabled = isConfirmed;
                qtyInput.setAttribute("aria-label", "Số lượng " + line.name);
                qtyInput.addEventListener("input", () => {
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
                incBtn.disabled = isConfirmed;
                incBtn.addEventListener("click", () => updateLineQuantity(line.code, line.qty + 1, true));

                const delBtn = document.createElement("button");
                delBtn.type = "button";
                delBtn.className = "pos-qty-del";
                delBtn.textContent = "Xóa";
                delBtn.setAttribute("aria-label", "Xóa " + line.name);
                delBtn.disabled = isConfirmed;
                delBtn.addEventListener("click", () => {
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
    }

    function updateLineQuantity(code, nextQty, rerender) {
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
        if (isConfirmed) {
            return;
        }
        const code = btn.dataset.maHang;
        const existing = cart.get(code);
        if (existing) {
            updateLineQuantity(code, existing.qty + 1, true);
            return;
        }
        cart.set(code, {
            code,
            name: btn.dataset.tenHang,
            unit: btn.dataset.dvt,
            price: Number(btn.dataset.gia),
            stock: Number(btn.dataset.ton),
            qty: 1,
            rawQty: "1",
            invalid: false
        });
        setError(cartError, "");
        renderCart();
    }

    function resetInvoice() {
        isConfirmed = false;
        isSubmitting = false;
        currentRequestId = (window.crypto && window.crypto.randomUUID)
            ? window.crypto.randomUUID().replaceAll("-", "")
            : String(Date.now()) + Math.random().toString(16).slice(2);
        cart.clear();
        cashAllocInput.value = "";
        cashGivenInput.value = "";
        transferInput.value = "";
        paymentMode = "TienMat";
        confirmResult.hidden = true;
        confirmBtn.disabled = false;
        confirmBtn.textContent = "Xác nhận bán hàng";
        draftStatus.textContent = "Nháp · Chưa xác nhận";
        setInputsDisabled(false);
        clearAllErrors();
        applyModeUI();
        renderCart();
        filterProducts();
    }

    function setInputsDisabled(disabled) {
        cashAllocInput.disabled = disabled;
        cashGivenInput.disabled = disabled;
        transferInput.disabled = disabled;
        fillCashBtn.disabled = disabled;
        fillTransferBtn.disabled = disabled;
        for (const btn of modeButtons) {
            btn.disabled = disabled;
        }
        for (const btn of denomButtons) {
            btn.disabled = disabled;
        }
    }

    async function handleConfirm() {
        clearAllErrors();

        if (!shiftToggle.checked) {
            setError(paymentError, "Ca bán hiện tại đã đóng. Vui lòng mở ca trước khi xác nhận bán hàng.");
            return;
        }

        if (cart.size === 0) {
            setError(cartError, "Giỏ hàng đang trống. Vui lòng chọn ít nhất một mặt hàng.");
            return;
        }

        const lines = [];
        for (const line of cart.values()) {
            if (line.invalid || !Number.isInteger(line.qty) || line.qty <= 0) {
                setError(cartError, `Số lượng của ${line.name} không hợp lệ (cần số nguyên dương).`);
                return;
            }
            if (line.qty > line.stock) {
                setError(cartError, `Số lượng của ${line.name} vượt tồn mẫu hiện có (${line.stock} ${line.unit}).`);
                return;
            }
            lines.push({ maHang: line.code, soLuong: line.qty });
        }

        const p = getPaymentNumbers();
        if (!p.cashAllocParsed.valid || !p.transfer.valid || !p.cashGiven.valid) {
            setError(paymentError, "Số tiền thanh toán phải là số nguyên VND không âm.");
            return;
        }

        const allocated = p.cashAlloc + p.transfer.value;
        if (allocated !== p.due) {
            setError(paymentError, `Tổng phân bổ (${formatMoney(allocated)}) phải bằng đúng giá trị hóa đơn (${formatMoney(p.due)}).`);
            return;
        }

        if (p.cashAlloc > 0 && (p.cashGiven.empty || p.cashGiven.value < p.cashAlloc)) {
            setError(cashGivenError, `Tiền mặt khách đưa phải lớn hơn hoặc bằng tiền mặt phân bổ (${formatMoney(p.cashAlloc)}).`);
            return;
        }

        if (isSubmitting) {
            return;
        }

        isSubmitting = true;
        confirmBtn.disabled = true;
        confirmBtn.textContent = "Đang xác nhận...";

        try {
            const payload = {
                maYeuCau: currentRequestId,
                cheDoThanhToan: paymentMode,
                tienMatPhanBo: p.cashAlloc,
                tienKhachDua: p.cashAlloc > 0 ? p.cashGiven.value : null,
                tienChuyenKhoan: p.transfer.value,
                caDangMo: shiftToggle.checked,
                dongBans: lines
            };

            const res = await fetch(confirmUrl, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            const data = await res.json();
            if (!res.ok || !data.ok) {
                const msg = data.message || "Không thể xác nhận hóa đơn.";
                if (data.field === "cart") {
                    setError(cartError, msg);
                } else if (data.field === "cash-given") {
                    setError(cashGivenError, msg);
                } else {
                    setError(paymentError, msg);
                }
                confirmBtn.disabled = false;
                confirmBtn.textContent = "Xác nhận bán hàng";
                return;
            }

            isConfirmed = true;
            draftStatus.textContent = "Đã xác nhận · " + data.maHoaDon;
            confirmedCodeEl.textContent = data.maHoaDon;
            confirmedDetailEl.textContent =
                `Tổng thu: ${formatMoney(data.tongTien)} (Tiền mặt: ${formatMoney(data.tienMatPhanBo)}, Chuyển khoản: ${formatMoney(data.tienChuyenKhoan)}) · Tiền thừa: ${formatMoney(data.tienThua)}`;
            lookupLink.href = data.traCuuUrl;
            printLink.href = data.inUrl;
            confirmResult.hidden = false;
            confirmBtn.textContent = "Đã xác nhận (" + data.maHoaDon + ")";
            setInputsDisabled(true);
            renderCart();
            filterProducts();
        } catch {
            setError(paymentError, "Mất kết nối tới máy chủ cục bộ khi xác nhận hóa đơn.");
            confirmBtn.disabled = false;
            confirmBtn.textContent = "Xác nhận bán hàng";
        } finally {
            isSubmitting = false;
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

    for (const btn of modeButtons) {
        btn.addEventListener("click", () => {
            if (isConfirmed) {
                return;
            }
            paymentMode = btn.dataset.mode;
            applyModeUI();
        });
    }

    cashAllocInput.addEventListener("input", () => {
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
        if (paymentMode === "HonHop") {
            const parsed = parseMoneyInput(transferInput);
            const due = getGrandTotal();
            if (parsed.valid && parsed.value <= due) {
                cashAllocInput.value = String(due - parsed.value);
            }
        }
        updateTotalsOnly();
    });

    cashGivenInput.addEventListener("input", updateTotalsOnly);

    fillCashBtn.addEventListener("click", () => {
        const due = getGrandTotal();
        const tr = parseMoneyInput(transferInput);
        const rem = Math.max(0, due - (tr.valid ? tr.value : 0));
        cashAllocInput.value = String(rem);
        updateTotalsOnly();
    });

    fillTransferBtn.addEventListener("click", () => {
        const due = getGrandTotal();
        const ca = parseMoneyInput(cashAllocInput);
        const rem = Math.max(0, due - (ca.valid ? ca.value : 0));
        transferInput.value = String(rem);
        updateTotalsOnly();
    });

    for (const btn of denomButtons) {
        btn.addEventListener("click", () => {
            if (isConfirmed) {
                return;
            }
            const p = getPaymentNumbers();
            const val = btn.dataset.denom === "exact" ? p.cashAlloc : Number(btn.dataset.denom);
            cashGivenInput.value = String(val);
            updateTotalsOnly();
        });
    }

    shiftToggle.addEventListener("change", () => {
        if (shiftToggle.checked) {
            shiftBadge.textContent = "Ca đang mở · S-CA-1126";
            shiftBadge.classList.add("pos-pill-teal");
            setError(paymentError, "");
        } else {
            shiftBadge.textContent = "Ca đã đóng (giả lập)";
            shiftBadge.classList.remove("pos-pill-teal");
        }
    });

    confirmBtn.addEventListener("click", handleConfirm);
    clearCartBtn.addEventListener("click", resetInvoice);
    newInvoiceBtn.addEventListener("click", resetInvoice);

    applyModeUI();
    renderCart();
    filterProducts();
})();
