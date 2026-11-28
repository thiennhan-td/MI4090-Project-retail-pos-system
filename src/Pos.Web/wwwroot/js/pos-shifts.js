(() => {
    const notes = [...document.querySelectorAll('[data-note]')];
    const counted = document.querySelector('input[name=Dem]');
    if (counted) {
        const expected = Number(document.querySelector('#expected').dataset.value);
        const delta = document.querySelector('#delta');
        function update() {
            delta.textContent = counted.value && counted.validity.valid
                ? (counted.valueAsNumber - expected).toLocaleString('vi-VN') + ' đ' : '—';
        }
        notes.forEach(note => note.addEventListener('input', () => {
            counted.value = notes.reduce((sum, input) => sum + Number(input.value) * Number(input.dataset.note), 0);
            update();
        }));
        counted.addEventListener('input', () => {
            notes.forEach(note => note.value = '');
            update();
        });
    }
    for (const form of document.querySelectorAll('[data-shift]')) {
        const notice = document.createElement('p');
        notice.setAttribute('role','status');
        form.after(notice);
        form.addEventListener('submit', async event => {
            event.preventDefault();
            if (form.dataset.busy) return;
            const payload = new FormData(form);
            const controls = [...form.elements].filter(c => !c.disabled);
            form.dataset.busy = 'true';
            controls.forEach(c => c.disabled = true);
            try {
                const response = await fetch(form.action,{method:'POST',body:payload});
                const result = await response.json();
                if (!response.ok) {
                    notice.textContent = result.message || 'Kiểm tra lại dữ liệu đã nhập.';
                    return;
                }
                const url = new URL(location.href);
                if (form.action.endsWith('/Mo')) url.searchParams.set('Chon',result.message);
                location.assign(url.href);
            } catch {
                notice.textContent = 'Chưa nhận được kết quả. Hãy kiểm tra lại giao dịch.';
            } finally {
                delete form.dataset.busy;
                controls.forEach(c => c.disabled = false);
            }
        });
    }
})();
