const salesFixtures = [
  { code: 'HD-2025-0142', shift: 'afternoon', sale: true, cash: 20000, transfer: 30000 },
  { code: 'PT-2025-0013', shift: 'afternoon', sale: false, cash: 60000, transfer: 0 },
  { code: 'PT-2025-0012', shift: 'morning', sale: false, cash: 40000, transfer: 0 },
  { code: 'HD-2025-0131', shift: 'morning', sale: true, cash: 0, transfer: 150000 },
];
const reportShift = document.querySelector('#report-shift');

function reportRow(values) {
  const row = document.createElement('tr');
  for (const value of values) row.insertCell().textContent = value;
  return row;
}

function renderSalesReport() {
  const visible = salesFixtures.filter(item => reportShift.value === 'all' || item.shift === reportShift.value);
  const paid = visible.filter(item => item.sale);
  const refunded = visible.filter(item => !item.sale);
  const sum = (items, property) => items.reduce((total, item) => total + item[property], 0);
  const gross = sum(paid, 'cash') + sum(paid, 'transfer');
  const refunds = sum(refunded, 'cash') + sum(refunded, 'transfer');
  document.querySelector('#report-count').textContent = new Set(paid.map(item => item.code)).size;
  document.querySelector('#report-sales').textContent = formatDemoMoney(gross);
  document.querySelector('#report-returns').textContent = formatDemoMoney(refunds);
  document.querySelector('#report-net').textContent = formatDemoMoney(gross - refunds);
  const chart = document.querySelector('#report-chart');
  const methods = document.querySelector('#report-methods');
  chart.replaceChildren();
  methods.replaceChildren();
  for (const [key, label] of [['cash', 'Tiền mặt'], ['transfer', 'Chuyển khoản']]) {
    const sales = sum(paid, key);
    const returns = sum(refunded, key);
    methods.append(reportRow([label, formatDemoMoney(sales), formatDemoMoney(returns), formatDemoMoney(sales - returns)]));
    const bar = document.createElement('label');
    const meter = document.createElement('progress');
    meter.max = Math.max(gross, 1);
    meter.value = sales;
    bar.className = 'demo-summary';
    bar.append(label + ' · Thu bán ' + formatDemoMoney(sales), meter);
    chart.append(bar);
  }
  document.querySelector('#report-vouchers tbody').replaceChildren(...visible.map(item => {
    const signed = item.sale ? 1 : -1;
    return reportRow([item.code, item.shift === 'morning' ? 'Ca sáng' : 'Ca chiều', item.sale ? 'Bán' : 'Trả',
      formatDemoMoney(signed * item.cash), formatDemoMoney(signed * item.transfer),
      formatDemoMoney(signed * (item.cash + item.transfer))]);
  }));
}
reportShift.addEventListener('change', renderSalesReport);
renderSalesReport();
