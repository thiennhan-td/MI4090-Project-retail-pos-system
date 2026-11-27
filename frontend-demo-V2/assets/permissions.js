const permissionRows = [...document.querySelectorAll('[data-permission]')];
const permissionChecks = [...document.querySelectorAll('[data-role]')];
const permissionStatus = document.querySelector('#permission-status');

function updatePermissionCounts() {
  const counts = permissionChecks.reduce((roles, check) => {
    const role = check.dataset.role;
    roles[role] = (roles[role] ?? 0) + Number(check.checked);
    return roles;
  }, {});
  document.querySelector('#permission-counts').textContent =
    'Thu ngân ' + counts.ThuNgan + '/40 · Nhân viên kho ' + counts.NhanVienKho + '/40 · Quản lý ' + counts.QuanLy + '/40';
}

document.querySelector('#permission-search').addEventListener('input', event => {
  const query = event.target.value.trim().toLocaleLowerCase('vi');
  for (const row of permissionRows) row.hidden = !row.textContent.toLocaleLowerCase('vi').includes(query);
  document.querySelector('#permission-empty').hidden = permissionRows.some(row => !row.hidden);
});
for (const check of permissionChecks) check.addEventListener('change', () => {
  updatePermissionCounts();
  permissionStatus.textContent = 'Có thay đổi chưa lưu.';
});
document.querySelector('#permission-save').addEventListener('click', () => {
  permissionStatus.textContent = 'Đã áp dụng cấu hình trong màn hình demo này.';
});
document.querySelector('#permission-reset').addEventListener('click', () => {
  for (const check of permissionChecks) check.checked = check.defaultChecked;
  updatePermissionCounts();
  permissionStatus.textContent = 'Đã khôi phục các quyền theo seed.';
});
updatePermissionCounts();
