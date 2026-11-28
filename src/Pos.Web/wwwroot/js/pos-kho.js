const receipt = document.querySelector('[data-kho-receipt]');
if (receipt) {
    const lines = receipt.querySelector('[data-kho-lines]');
    const add = receipt.querySelector('[data-kho-add]');
    const template = receipt.querySelector('[data-kho-template]');
    const total = receipt.querySelector('[data-kho-total]');
    const formatter = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 });

    function wholeNumber(field) {
        return field.validity.valid && /^[0-9]+$/.test(field.value) ? BigInt(field.value) : 0n;
    }

    function updateLines() {
        const rows = [...lines.rows];
        let sum = 0n;
        rows.forEach((row, index) => {
            row.querySelectorAll('[data-kho-field]').forEach(field => {
                field.name = 'Dongs[' + index + '].' + field.dataset.khoField;
                field.removeAttribute('id');
            });
            const quantity = wholeNumber(row.querySelector('[data-kho-field="SoLuong"]'));
            const price = wholeNumber(row.querySelector('[data-kho-field="DonGia"]'));
            sum += quantity * price;
            row.querySelector('[data-kho-remove]').disabled = rows.length === 1;
        });
        add.disabled = rows.length >= 100;
        total.value = formatter.format(sum);
    }

    add.addEventListener('click', () => {
        lines.append(template.content.cloneNode(true));
        updateLines();
        lines.rows[lines.rows.length - 1].querySelector('select').focus();
    });
    lines.addEventListener('click', event => {
        if (event.target.closest('[data-kho-remove]') && lines.rows.length > 1) {
            event.target.closest('tr').remove();
            updateLines();
            add.focus();
        }
    });
    lines.addEventListener('input', updateLines);
    updateLines();
}

const payment = document.querySelector('[data-kho-payment]');
if (payment) {
    const source = payment.querySelector('[data-kho-source]');
    const shift = payment.querySelector('[data-kho-shift]');
    const method = payment.elements.PhuongThuc;
    function updateSource() {
        const usesShift = source.value === 'KetCa';
        shift.disabled = !usesShift;
        shift.required = usesShift;
        method.querySelector('[value="ChuyenKhoan"]').disabled = usesShift;
        if (usesShift) method.value = 'TienMat';
        else shift.value = '';
    }
    source.addEventListener('change', updateSource);
    updateSource();
}
