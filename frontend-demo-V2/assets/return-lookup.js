(() => {
  const get = id => document.getElementById(id);
  const search = get('return-search');
  const shift = get('return-shift');
  const date = get('return-date');
  const method = get('return-method');
  const result = get('return-result');
  const detail = get('return-detail');
  const original = get('return-original');
  const entries = Array.from(get('return-table').tBodies[0].rows, row => {
    const select = row.querySelector('[data-return-select]');
    const [time, sampleDate] = row.cells[1].querySelector('span').textContent.trim().split(/\s+/);
    const [day, month, year] = sampleDate.split('/');
    const [amount, payment] = row.cells[3].querySelectorAll('span');
    return {
      row,
      select,
      code: select.textContent.trim(),
      invoice: row.cells[0].querySelector('.font-mono').textContent.trim(),
      timestamp: time + ' · ' + sampleDate,
      date: [year, month, day].join('-'),
      shift: row.cells[1].querySelector('span:last-child').textContent.trim(),
      stock: row.cells[2].querySelector('span:last-child').textContent.trim(),
      amount: amount.textContent.trim(),
      payment: payment.textContent.trim(),
      status: row.cells[4].textContent.trim()
    };
  });
  let selected = null;

  function selectReturn(entry) {
    selected = entry;
    for (const item of entries) {
      item.row.classList.toggle('return-selected', item === selected);
      item.select.setAttribute('aria-pressed', String(item === selected));
    }
    detail.hidden = !entry;
    if (!entry) return;

    get('return-detail-heading').textContent = entry.code;
    get('return-detail-status').textContent = entry.status;
    get('return-detail-date').textContent = entry.timestamp;
    get('return-detail-shift').textContent = entry.shift;
    get('return-detail-stock').textContent = entry.stock;
    get('return-detail-method').textContent = entry.payment;
    get('return-detail-total').textContent = entry.amount;
    get('return-detail-items').hidden = entry.code !== 'PT-2025-0013';
    original.textContent = entry.invoice;
    if (entry.select.hasAttribute('data-return-invoice-available')) {
      const target = '../F3.4_TraInHuyHoaDon/code.html?maHoaDon=' + encodeURIComponent(entry.invoice);
      original.href = target;
      original.dataset.demoHref = target;
    } else {
      original.removeAttribute('href');
      delete original.dataset.demoHref;
    }
  }

  function filterReturns() {
    const query = search.value.trim().toUpperCase();
    for (const entry of entries) {
      entry.row.hidden = !(entry.code.includes(query) || entry.invoice.includes(query))
        || !entry.shift.toLocaleLowerCase('vi-VN').includes(shift.value)
        || !entry.payment.toLocaleLowerCase('vi-VN').includes(method.value)
        || Boolean(date.value && entry.date !== date.value);
    }
    const visible = entries.filter(entry => !entry.row.hidden);
    result.textContent = visible.length
      ? 'Có ' + visible.length + ' phiếu khớp trong dữ liệu mẫu.'
      : 'Không tìm thấy phiếu trả.';
    selectReturn(visible.includes(selected) ? selected : visible[0] ?? null);
  }

  for (const entry of entries) {
    entry.row.addEventListener('click', () => selectReturn(entry));
  }
  get('return-filter').addEventListener('click', filterReturns);
  get('return-clear-search').addEventListener('click', () => {
    search.value = '';
    filterReturns();
    search.focus();
  });
  for (const button of document.querySelectorAll('[data-reset-returns]')) {
    button.addEventListener('click', () => {
      search.value = '';
      shift.value = '';
      date.value = '';
      method.value = '';
      filterReturns();
    });
  }
  search.addEventListener('keydown', event => {
    if (event.key === 'Enter') filterReturns();
  });
  filterReturns();
})();
