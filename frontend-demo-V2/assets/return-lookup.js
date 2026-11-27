const returnSearch = document.querySelector('#return-search');
const returnRows = [...document.querySelectorAll('#return-table tbody tr')];
const returnShift = document.querySelector('#return-shift');
const returnMethod = document.querySelector('#return-method');
const returnResult = document.createElement('p');
returnResult.className = 'demo-status';
returnResult.setAttribute('role', 'status');
document.querySelector('#return-table').parentElement.after(returnResult);

function filterReturns() {
  const query = returnSearch.value.trim().toLocaleLowerCase('vi');
  for (const row of returnRows) {
    const rowText = row.textContent.toLocaleLowerCase('vi');
    row.hidden = !rowText.includes(query)
      || !row.cells[1].textContent.toLocaleLowerCase('vi').includes(returnShift.value)
      || !row.cells[3].textContent.toLocaleLowerCase('vi').includes(returnMethod.value);
  }
  const shown = returnRows.filter(row => !row.hidden).length;
  returnResult.textContent = shown ? 'Có ' + shown + ' phiếu khớp trong dữ liệu mẫu.' : 'Không tìm thấy phiếu trả.';
}

document.querySelector('#return-filter').addEventListener('click', filterReturns);
for (const button of document.querySelectorAll('[data-reset-returns]')) {
  button.addEventListener('click', () => {
    returnSearch.value = '';
    returnShift.value = '';
    returnMethod.value = '';
    filterReturns();
  });
}
returnSearch.addEventListener('keydown', event => {
  if (event.key === 'Enter') filterReturns();
});
filterReturns();
