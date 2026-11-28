(() => {
    const notes = [...document.querySelectorAll("[data-note]")];
    const counted = document.querySelector("input[name=Dem]");
    if (counted) {
        const expected = Number(document.querySelector("#expected").dataset.value);
        const delta = document.querySelector("#delta");

        function update() {
            delta.textContent = counted.value && counted.validity.valid
                ? (counted.valueAsNumber - expected).toLocaleString("vi-VN") + " đ" : "—";
        }

        notes.forEach(note => note.addEventListener("input", () => {
            counted.value = notes.every(input => input.validity.valid)
                ? notes.reduce((sum, input) => sum + Number(input.value) * Number(input.dataset.note), 0)
                : "";
            update();
        }));
        counted.addEventListener("input", () => {
            notes.forEach(note => { note.value = ""; });
            update();
        });
    }

    for (const form of document.querySelectorAll("[data-shift]")) {
        const source = form.elements.namedItem("Nguon");
        const method = form.elements.namedItem("PhuongThuc");
        if (source && method) {
            const transfer = method.querySelector('option[value="ChuyenKhoan"]');
            function syncPaymentMethod() {
                transfer.disabled = source.value === "KetCa";
                if (transfer.disabled) method.value = "TienMat";
            }
            source.addEventListener("change", syncPaymentMethod);
            syncPaymentMethod();
        }

        const feedback = document.createElement("div");
        feedback.hidden = true;
        const notice = document.createElement("p");
        notice.setAttribute("role", "status");
        const retry = document.createElement("button");
        retry.type = "button";
        retry.className = "pos-btn pos-btn-secondary";
        retry.textContent = "Thử lại đúng giao dịch";
        const retryAction = document.createElement("span");
        retryAction.hidden = true;
        retryAction.append(retry);
        const signIn = document.createElement("a");
        signIn.className = "pos-btn pos-btn-secondary";
        signIn.textContent = "Đăng nhập lại";
        const signInAction = document.createElement("span");
        signInAction.hidden = true;
        signInAction.append(signIn);
        feedback.append(notice, retryAction, signInAction);
        form.after(feedback);

        let pending = null;
        let busy = false;

        async function send() {
            if (!pending || busy) return;
            busy = true;
            feedback.hidden = false;
            retryAction.hidden = true;
            signInAction.hidden = true;
            notice.textContent = "Đang gửi…";
            try {
                const response = await fetch(form.action, {
                    method: "POST",
                    body: pending.payload,
                    signal: AbortSignal.timeout(15000)
                });
                const isJson = response.headers.get("content-type")?.includes("application/json");
                const result = isJson ? await response.json().catch(() => null) : null;
                if (response.status >= 500 || (response.ok && !result?.message)) {
                    throw new Error("Missing operation result");
                }
                if (!response.ok) {
                    notice.textContent = result?.message || "Yêu cầu bị từ chối. Kiểm tra dữ liệu hoặc tải lại biểu mẫu.";
                    if (response.status === 401 && form.dataset.loginUrl) {
                        signIn.href = form.dataset.loginUrl;
                        signInAction.hidden = false;
                    }
                    pending.controls.forEach(control => { control.disabled = false; });
                    pending.submitter?.focus();
                    pending = null;
                    return;
                }

                const url = new URL(location.href);
                if (form.dataset.shift === "ca") {
                    for (const key of [...url.searchParams.keys()]) {
                        if (key.toLowerCase() === "chon") url.searchParams.delete(key);
                    }
                    url.searchParams.set("chon", result.message);
                }
                location.assign(url.href);
            } catch {
                notice.textContent = "Chưa nhận được kết quả. Thử lại sẽ gửi nguyên dữ liệu của lần trước.";
                retryAction.hidden = false;
                retry.focus();
            } finally {
                busy = false;
            }
        }

        form.addEventListener("submit", event => {
            event.preventDefault();
            if (pending) return;
            pending = {
                payload: new FormData(form),
                controls: [...form.elements].filter(control => !control.disabled),
                submitter: event.submitter
            };
            pending.controls.forEach(control => { control.disabled = true; });
            send();
        });
        retry.addEventListener("click", send);
    }
})();
