if (window.self !== window.top) document.body.classList.add('demo-embedded');

const demoMoney = new Intl.NumberFormat('vi-VN');
const formatDemoMoney = amount => demoMoney.format(amount || 0) + ' đ';

document.fonts.load('16px "Material Symbols Outlined"', 'close').then(fonts => {
  if (fonts.length) return;
  for (const button of document.querySelectorAll('button:has(.material-symbols-outlined)')) {
    const hasText = [...button.childNodes].some(node =>
      node.textContent.trim() && !node.classList?.contains('material-symbols-outlined'));
    const label = button.getAttribute('aria-label') || button.title;
    if (!hasText && label) button.dataset.iconLabel = label;
  }
  document.body.classList.add('demo-icons-unavailable');
}).catch(() => {});

for (const control of document.querySelectorAll('[data-demo-href]')) {
  control.addEventListener('click', event => {
    if (control.matches('a') && (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey)) return;
    event.preventDefault();
    const target = new URL(control.dataset.demoHref, location.href);
    if (window.self === window.top) {
      location.href = target.href;
    } else {
      const screen = target.pathname.split('/').at(-2);
      const gallery = new URL('../../index.html', location.href);
      gallery.hash = screen + target.search;
      window.top.location = gallery.href;
    }
  });
}

function downloadDemoTable(table, filename) {
  const rows = [...table.rows].filter(row => !row.hidden && row.style.display !== 'none');
  const quote = value => '"' + value.replaceAll('"', '""') + '"';
  const csv = rows.map(row => [...row.cells].filter(cell => cell.dataset.csvIgnore === undefined)
    .map(cell => quote(cell.innerText.trim().replace(/\s+/g, ' '))).join(',')).join('\r\n');
  const url = URL.createObjectURL(new Blob(['\ufeff', csv], { type: 'text/csv;charset=utf-8' }));
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

for (const button of document.querySelectorAll('[data-csv-table]')) {
  button.addEventListener('click', () => downloadDemoTable(document.querySelector(button.dataset.csvTable), button.dataset.csvFile));
}
