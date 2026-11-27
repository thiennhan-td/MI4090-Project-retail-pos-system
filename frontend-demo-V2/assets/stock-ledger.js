const sourceScreens = {
  PN: 'F5.1-F5.2_LapNhapKho',
  HD: 'F3.4_TraInHuyHoaDon',
  PT: 'F4.3_TraCuuPhieuTra',
  KK: 'F6.2-F6.4_KiemKho',
};
document.querySelector('#stock-source-link').addEventListener('click', () => {
  const prefix = document.querySelector('#modalDocCode').textContent.trim().split('-')[0];
  const id = sourceScreens[prefix];
  if (!id) return;
  if (window.self === window.top) location.href = '../' + id + '/code.html';
  else window.top.location = new URL('../../index.html#' + id, location.href).href;
});

const ledgerType = document.querySelector('#stock-ledger-type');
const ledgerRows = [...document.querySelectorAll('#stock-ledger tbody tr')];
ledgerType.addEventListener('change', () => {
  const term = ledgerType.value === 'Điều chỉnh' ? 'Cân chỉnh' : ledgerType.value;
  for (const row of ledgerRows) {
    const movement = row.cells[2]?.textContent.trim() ?? '';
    row.hidden = movement !== 'Tồn đầu kỳ' && !movement.startsWith(term);
  }
});
