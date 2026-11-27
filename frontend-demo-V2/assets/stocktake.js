const stockRows = [...document.querySelectorAll('[data-stock-row]')];
const stockButtons = [...document.querySelectorAll('[data-stock-filter]')];
const stockRole = document.querySelector('#stock-role');
const stockState = document.querySelector('#stock-state');
const stockResult = document.querySelector('#stock-result');
const stockSubmit = document.querySelector('#stock-submit');
const stockApprove = document.querySelector('#stock-approve');
const stockPrint = document.querySelector('#stock-print');
let status = 'counting';
let activeFilter = 'all';

function refreshStocktake() {
  const counts = { all: stockRows.length, pending: 0, match: 0, different: 0 };
  const warehouseCanEdit = status === 'counting' && stockRole.value === 'warehouse';
  for (const row of stockRows) {
    const [quantity, reason] = row.querySelectorAll('input');
    const counted = quantity.value !== '' && quantity.checkValidity();
    const diff = quantity.valueAsNumber - Number(row.dataset.book);
    const state = !counted ? 'pending' : diff ? 'different' : 'match';
    counts[state]++;
    row.hidden = activeFilter !== 'all' && activeFilter !== state;
    row.querySelector('[data-stock-difference]').textContent = !counted ? 'Chưa đếm' : diff > 0 ? '+' + diff : String(diff);
    quantity.disabled = reason.disabled = !warehouseCanEdit;
    for (const button of row.querySelectorAll('button')) button.disabled = !warehouseCanEdit;
  }
  for (const button of stockButtons) {
    button.setAttribute('aria-pressed', String(activeFilter === button.dataset.stockFilter));
  }
  document.querySelector('#stock-counts').textContent = 'Khớp: ' + counts.match
    + ' · Có chênh lệch: ' + counts.different + ' · Chưa đếm: ' + counts.pending;
  stockState.textContent = { counting: 'Đang kiểm', review: 'Chờ duyệt', done: 'Hoàn tất' }[status];
  document.querySelector('[data-demo-role]').textContent = stockRole.selectedOptions[0].textContent;
  stockSubmit.hidden = !warehouseCanEdit;
  stockApprove.hidden = status !== 'review' || stockRole.value !== 'manager';
  stockPrint.hidden = status !== 'done';
}

for (const row of stockRows) {
  const quantity = row.querySelector('input');
  quantity.addEventListener('input', refreshStocktake);
  for (const button of row.querySelectorAll('[data-adjust]')) {
    button.addEventListener('click', () => {
      quantity.value = String(Math.max(0, (quantity.valueAsNumber || 0) + Number(button.dataset.adjust)));
      refreshStocktake();
    });
  }
}
for (const button of stockButtons) button.addEventListener('click', () => {
  activeFilter = button.dataset.stockFilter;
  refreshStocktake();
});
stockRole.addEventListener('change', refreshStocktake);
stockSubmit.addEventListener('click', () => {
  for (const row of stockRows) {
    const [quantity, reason] = row.querySelectorAll('input');
    if (quantity.value === '' || !quantity.checkValidity()
        || (quantity.valueAsNumber !== Number(row.dataset.book) && !reason.value.trim())) {
      activeFilter = 'all';
      refreshStocktake();
      stockResult.textContent = 'Nhập thực đếm hợp lệ và giải trình cho mọi dòng có chênh lệch.';
      (quantity.value === '' || !quantity.checkValidity() ? quantity : reason).focus();
      return;
    }
  }
  status = 'review';
  stockResult.textContent = 'Đã gửi phiếu mẫu. Chuyển sang vai trò Quản lý để duyệt.';
  refreshStocktake();
});
stockApprove.addEventListener('click', () => {
  if (status !== 'review' || stockRole.value !== 'manager') return;
  status = 'done';
  stockResult.textContent = 'Đã duyệt điều chỉnh tồn trong phiên demo.';
  refreshStocktake();
});
stockPrint.addEventListener('click', () => window.print());
refreshStocktake();
