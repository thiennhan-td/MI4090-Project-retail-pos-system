const refundRows = [...document.querySelectorAll('[data-refund-item]')];
const refundAmounts = [...document.querySelectorAll('#refund-amounts input')];
const refundDraft = document.querySelector('#refund-draft');
const refundStatus = document.querySelector('#refund-status');
const lookup = document.querySelector('#refund-lookup');
const refundConfirm = document.querySelector('#refund-confirm');
const refundSave = document.querySelector('#refund-save');
const refundDelete = document.querySelector('#refund-delete');
const refundInputs = [...refundDraft.querySelectorAll('input')];
const refundDraftKey = 'retail-core:refund:HD-2025-0142';
let refundConfirmed = false;

function updateRefund() {
  let total = 0;
  let valid = true;
  for (const row of refundRows) {
    const [returned, restocked] = row.querySelectorAll('input');
    const qty = returned.valueAsNumber || 0;
    const max = Number(row.dataset.max);
    restocked.max = String(Math.max(0, Math.min(qty, max)));
    valid &&= returned.checkValidity() && restocked.checkValidity();
    const subtotal = Math.max(0, qty) * Number(row.dataset.price);
    total += subtotal;
    row.querySelector('[data-subtotal]').textContent = formatDemoMoney(subtotal);
  }
  valid &&= refundAmounts.every(field => field.checkValidity());
  const allocated = refundAmounts.reduce((sum, field) => sum + (field.valueAsNumber || 0), 0);
  document.querySelector('#refund-total').textContent = formatDemoMoney(total);
  document.querySelector('#refund-balance').textContent = formatDemoMoney(total - allocated);
  return { total, allocated, valid };
}

function resetRefund() {
  for (const input of refundDraft.querySelectorAll('input')) {
    input.disabled = false;
    input.value = '0';
  }
  refundConfirmed = false;
  refundConfirm.disabled = false;
  refundSave.disabled = false;
  refundDelete.disabled = false;
  refundStatus.classList.remove('demo-error');
  refundStatus.textContent = '';
  updateRefund();
}

for (const input of refundDraft.querySelectorAll('input')) {
  input.addEventListener('input', () => {
    if (!refundConfirmed) {
      refundStatus.textContent = '';
      updateRefund();
    }
  });
}
document.querySelector('#refund-search').addEventListener('submit', event => {
  event.preventDefault();
  const found = document.querySelector('#invoice-input').value.trim().toUpperCase() === 'HD-2025-0142';
  refundDraft.hidden = !found;
  lookup.textContent = found ? 'Hóa đơn HD-2025-0142 · 25/10/2025'
    : 'Không tìm thấy. Thử mã hóa đơn mẫu HD-2025-0142.';
});
refundConfirm.addEventListener('click', () => {
  const { total, allocated, valid } = updateRefund();
  if (!valid || !total || total !== allocated) {
    refundStatus.classList.add('demo-error');
    refundStatus.textContent = 'Kiểm tra số lượng được trả, số nhập lại và phân bổ đủ số tiền hoàn.';
    return;
  }
  refundConfirmed = true;
  for (const input of refundDraft.querySelectorAll('input')) input.disabled = true;
  refundConfirm.disabled = true;
  refundSave.disabled = true;
  refundDelete.disabled = true;
  try { sessionStorage.removeItem(refundDraftKey); } catch {}
  refundStatus.classList.remove('demo-error');
  refundStatus.textContent = 'Đã xác nhận phiếu trả mẫu. Số nhập lại và khoản hoàn đã được kiểm tra.';
});
refundSave.addEventListener('click', () => {
  if (!updateRefund().valid) {
    refundStatus.classList.add('demo-error');
    refundStatus.textContent = 'Sửa số lượng chưa hợp lệ trước khi lưu nháp.';
    return;
  }
  try {
    sessionStorage.setItem(refundDraftKey, JSON.stringify(refundInputs.map(input => input.value)));
    refundStatus.classList.remove('demo-error');
    refundStatus.textContent = 'Đã lưu phiếu nháp trong phiên demo này.';
  } catch {
    refundStatus.textContent = 'Trình duyệt không cho lưu nháp trong phiên này.';
  }
});
refundDelete.addEventListener('click', () => {
  if (!window.confirm('Xóa các dòng hàng và khoản hoàn trên phiếu nháp này?')) return;
  try { sessionStorage.removeItem(refundDraftKey); } catch {}
  resetRefund();
  refundStatus.textContent = 'Đã xóa nội dung phiếu nháp.';
});
try {
  const saved = JSON.parse(sessionStorage.getItem(refundDraftKey));
  if (Array.isArray(saved) && saved.length === refundInputs.length) {
    refundInputs.forEach((input, index) => { input.value = saved[index]; });
  }
} catch {}
updateRefund();
