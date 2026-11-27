const screens = JSON.parse(document.querySelector('#screen-data').textContent);
const frame = document.querySelector('#screen-frame');
const dropdown = document.querySelector('#screen-picker');
const links = [...document.querySelectorAll('[data-screen]')];
let currentScreen;

function renderScreen() {
  const screen = screens.find(item => item.id === location.hash.slice(1)) ?? screens[0];
  if (currentScreen === screen) return;
  currentScreen = screen;
  const title = screen.code + ' · ' + screen.title;
  document.querySelector('#current-title').textContent = title;
  document.querySelector('#screen-open').href = screen.html;
  const original = document.querySelector('#screen-original');
  original.hidden = !screen.original;
  if (screen.original) original.href = screen.img;
  else original.removeAttribute('href');
  if (frame.getAttribute('src') !== screen.html) frame.src = screen.html;
  frame.title = title;
  dropdown.value = screen.id;
  for (const link of links) {
    if (link.dataset.screen === screen.id) link.setAttribute('aria-current', 'page');
    else link.removeAttribute('aria-current');
  }
}

for (const link of links) {
  link.addEventListener('click', event => {
    if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
    event.preventDefault();
    location.hash = link.dataset.screen;
  });
}
dropdown.addEventListener('change', () => { location.hash = dropdown.value; });
window.addEventListener('hashchange', renderScreen);
renderScreen();
