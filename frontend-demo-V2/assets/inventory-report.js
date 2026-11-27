const inventoryCategory = document.querySelector('#filter-category');
const inventorySearch = document.querySelector('#search-sku');
const inventoryRows = [...document.querySelectorAll('.xnt-row')];

function inventoryNumber(cell) {
  return Number(cell.textContent.trim().split(/\s+/)[0].replaceAll('.', '')) || 0;
}

function updateInventoryReport() {
  const query = inventorySearch.value.trim().toLocaleLowerCase('vi');
  const selected = inventoryCategory.value;
  const totals = { opening: 0, in: 0, out: 0, closing: 0 };
  let count = 0;
  for (const row of inventoryRows) {
    row.hidden = (selected !== 'all' && row.dataset.category !== selected)
      || !row.dataset.search.toLocaleLowerCase('vi').includes(query);
    if (row.hidden) continue;
    count++;
    const [opening, purchases, sales, returned, adjustment, closing] = [...row.cells].slice(2).map(inventoryNumber);
    totals.opening += opening;
    totals.in += purchases + returned + Math.max(0, adjustment);
    totals.out += -sales + Math.max(0, -adjustment);
    totals.closing += closing;
  }
  for (const [key, value] of Object.entries(totals)) {
    const sign = key === 'in' && value ? '+' : key === 'out' && value ? '−' : '';
    document.querySelector('#xnt-' + key).textContent = sign + demoMoney.format(value);
  }
  document.querySelector('#table-count-badge').textContent = count ? 'Hiển thị ' + count + ' mặt hàng' : 'Không có mặt hàng khớp';
}
inventoryCategory.addEventListener('change', updateInventoryReport);
inventorySearch.addEventListener('input', updateInventoryReport);
document.querySelector('#btn-print-report').addEventListener('click', () => window.print());
updateInventoryReport();
