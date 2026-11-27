for (const search of document.querySelectorAll('[data-catalog-find]')) {
  const panel = document.getElementById(search.dataset.catalogFind);
  const table = panel.querySelector('table');
  const empty = document.createElement('p');
  empty.className = 'demo-status';
  empty.setAttribute('role', 'status');
  table.closest('.demo-table').after(empty);
  const filter = () => {
    const query = search.value.trim().toLocaleLowerCase('vi');
    for (const row of table.tBodies[0].rows) {
      row.hidden = !row.textContent.toLocaleLowerCase('vi').includes(query);
    }
    empty.textContent = [...table.tBodies[0].rows].some(row => !row.hidden)
      ? '' : 'Không tìm thấy bản ghi khớp.';
  };
  search.addEventListener('input', filter);
  for (const button of panel.querySelectorAll('button')) {
    if (button.textContent.trim() === 'Lọc') button.addEventListener('click', filter);
  }
}
