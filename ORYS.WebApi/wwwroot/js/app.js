/* ================================================
   AFM GRAND HOTEL — Ultra Premium JS v5
   ================================================ */
console.log(">>> PORTAL VERSION 5 LOADED <<<");

const API = window.location.origin;

// State Variables
let allRooms = [];
let filteredRooms = [];
let currentFilter = 'all';
let isSearchMode = false;
let checkIn = '';
let checkOut = '';
let nights = 0;
let selectedRoom = null;

// Premium Room Images (Specific to requested theme)
const ROOM_IMAGES = {
  'Standart': 'https://images.unsplash.com/photo-1611892440504-42a792e24d32?w=800&q=80', // Elegant, standard luxury
  'Deluxe':   'https://images.unsplash.com/photo-1590490360182-c33d57733427?w=800&q=80', // Sea view
  'Suite':    'https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=800&q=80', // High-end suite
  'Aile':     'https://images.unsplash.com/photo-1566665797739-1674de7a421a?w=800&q=80', // Spacious
  'Kral':     'https://images.unsplash.com/photo-1631049307264-da0ec9d70304?w=800&q=80', // VIP, mirrors, shiny
};

// Hero Slider Images
const HERO_IMAGES = [
  'url("https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?w=1920&q=80")', // Stunning exterior/pool
  'url("https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=1920&q=80")', // Luxury lobby
  'url("https://images.unsplash.com/photo-1618773928121-c32242e63f39?w=1920&q=80")'  // Premium room view
];

const FALLBACK_IMG = 'https://images.unsplash.com/photo-1512918728675-ed5a9ecdebfd?w=800&q=80';

// Initialize
document.addEventListener('DOMContentLoaded', () => {
  initDates();
  initHeaderScroll();
  startHeroSlider();
  loadAllRooms();
  checkPortalSession();

  // Listeners for reservation dates in modal
  getEl('rCheckIn').addEventListener('change', updateModalPrice);
  getEl('rCheckOut').addEventListener('change', updateModalPrice);
});

/* ── UI HELPERS ── */
function getEl(id) { return document.getElementById(id); }
function fmt(n) { return new Intl.NumberFormat('tr-TR').format(Math.round(n)); }
function getRoomImage(type) {
  for (let key in ROOM_IMAGES) if ((type||'').includes(key)) return ROOM_IMAGES[key];
  return FALLBACK_IMG;
}

/* ── HEADER & INIT ── */
function initHeaderScroll() {
  window.addEventListener('scroll', () => {
    getEl('header').classList.toggle('scrolled', window.scrollY > 50);
  });
}

function initDates() {
  const today = new Date();
  const tmrrw = new Date(today); tmrrw.setDate(today.getDate() + 1);
  const next  = new Date(today); next.setDate(today.getDate() + 3);

  const format = d => d.toISOString().split('T')[0];
  
  const ci = getEl('checkIn');
  const co = getEl('checkOut');
  
  ci.min = format(today);
  ci.value = format(tmrrw);
  
  co.min = format(tmrrw);
  co.value = format(next);

  ci.addEventListener('change', () => {
    const ciDate = new Date(ci.value);
    const minCo = new Date(ciDate); minCo.setDate(ciDate.getDate() + 1);
    co.min = format(minCo);
    if (new Date(co.value) <= ciDate) co.value = format(minCo);
  });
}

function scrollToRooms() {
  getEl('rooms-section').scrollIntoView({ behavior: 'smooth' });
}

function toggleMenu() {
  getEl('mobileNav').classList.toggle('open');
}

/* ── HERO SLIDER ── */
let currentSlide = 0;
function startHeroSlider() {
  getEl('heroBg').style.backgroundImage = HERO_IMAGES[0];
  setInterval(() => goSlide((currentSlide + 1) % HERO_IMAGES.length), 6000);
}
function goSlide(index) {
  currentSlide = index;
  getEl('heroBg').style.opacity = 0;
  setTimeout(() => {
    getEl('heroBg').style.backgroundImage = HERO_IMAGES[index];
    getEl('heroBg').style.opacity = 1;
  }, 400);

  document.querySelectorAll('.hslide-dot').forEach((d, i) => {
    d.classList.toggle('active', i === index);
  });
}

/* ── API & ROOMS ── */
async function loadAllRooms() {
  showLoading(true);
  try {
    const res = await fetch(`${API}/api/rooms/all`);
    if (!res.ok) throw new Error('Hata');
    allRooms = await res.json();
    applyFilter('all');
  } catch (e) {
    showToast('Odalar yüklenemedi.', 'error');
    showLoading(false);
  }
}

async function searchRooms() {
  const ci = getEl('checkIn').value;
  const co = getEl('checkOut').value;
  
  if (!ci || !co) return showToast('Lütfen tarih seçin', 'error');
  
  const d1 = new Date(ci), d2 = new Date(co);
  if (d2 <= d1) return showToast('Çıkış tarihi hatalı', 'error');
  
  checkIn = ci;
  checkOut = co;
  nights = Math.round((d2 - d1) / 86400000);
  
  const btn = getEl('searchBtn');
  getEl('searchBtnText').textContent = 'Aranıyor...';
  getEl('searchSpinner').classList.remove('hidden');
  btn.disabled = true;

  showLoading(true);
  
  try {
    const res = await fetch(`${API}/api/rooms/available?checkIn=${ci}&checkOut=${co}`);
    if (!res.ok) throw new Error('Hata');
    
    allRooms = await res.json();
    isSearchMode = true;
    
    const fmtD = d => new Date(d).toLocaleDateString('tr-TR', {day:'numeric',month:'long'});
    getEl('rooms-eyebrow').textContent = `${allRooms.length} Müsait Oda`;
    getEl('rooms-title').innerHTML = `Uygun <em>Seçenekleriniz</em>`;
    getEl('rooms-subtitle').innerHTML = `${fmtD(ci)} - ${fmtD(co)} • ${nights} Gece • <a href="javascript:resetSearch()" style="color:var(--gold);text-decoration:underline">Tümünü Göster</a>`;
    
    applyFilter(currentFilter);
    scrollToRooms();

  } catch (e) {
    showToast('Arama başarısız.', 'error');
    showLoading(false);
  } finally {
    getEl('searchBtnText').textContent = 'Müsait Oda Ara';
    getEl('searchSpinner').classList.add('hidden');
    btn.disabled = false;
  }
}

function resetSearch() {
  isSearchMode = false;
  nights = 0; checkIn = ''; checkOut = '';
  getEl('rooms-eyebrow').textContent = 'Oda Koleksiyonu';
  getEl('rooms-title').innerHTML = `Konforunuzu <em>Seçin</em>`;
  getEl('rooms-subtitle').textContent = 'Tarih seçerek müsait odaları filtreleyin ya da tüm odalarımıza göz atın';
  loadAllRooms();
}

function filterRooms(type) {
  currentFilter = type;
  document.querySelectorAll('.flt-btn').forEach(btn => {
    btn.classList.toggle('active', btn.dataset.f === type);
  });
  applyFilter(type);
}

function applyFilter(type) {
  filteredRooms = type === 'all' 
    ? [...allRooms] 
    : allRooms.filter(r => (r.roomTypeName||'').toLowerCase().includes(type.toLowerCase()));
    
  renderRooms(filteredRooms);
}

function renderRooms(rooms) {
  showLoading(false);
  const grid = getEl('rooms-grid');
  grid.innerHTML = '';
  
  if (rooms.length === 0) {
    getEl('no-rooms').classList.remove('hidden');
    return;
  }
  getEl('no-rooms').classList.add('hidden');

  rooms.forEach((r, i) => {
    // Oda müsaitlik kontrolü: Search modunda ise zaten müsaitler gelir, değilse status'e bak
    const isOccupied = r.status === 'Occupied';
    const isAvail = !isOccupied && r.status !== 'Maintenance';
    
    const imgUrl = getRoomImage(r.roomTypeName);
    
    const statusTag = isSearchMode 
      ? `<div class="room-status-tag ${isAvail ? 'avail' : 'busy'}">${isAvail ? '✓ Müsait' : '✗ Dolu'}</div>` 
      : (isOccupied ? `<div class="room-status-tag busy">✗ Dolu (İçeride Müşteri Var)</div>` : '');

    const priceHtml = isSearchMode && nights > 0
      ? `<div class="room-price-total">Toplam: ${fmt(r.pricePerNight * nights)} ₺</div>`
      : '';

    const card = document.createElement('div');
    card.className = `room-card fade-in-up ${isOccupied ? 'occupied' : ''}`;
    card.style.animationDelay = `${i * 0.05}s`;
    
    // Oda doluysa uyarı ver, değilse modalı aç
    card.onclick = () => {
      if (isOccupied) {
        showToast('Bu oda şu an dolu, içeride müşteri var!', 'error');
        return;
      }
      openFModal(r.id);
    };
    
    card.innerHTML = `
      <div class="room-img-zone">
        <div class="room-type-tag">✦ ${r.roomTypeName}</div>
        ${statusTag}
        <img class="room-card-img" src="${imgUrl}" alt="${r.roomTypeName}"/>
        <div class="room-img-gradient"></div>
        <div class="room-num-tag">Oda ${r.roomNumber}</div>
      </div>
      <div class="room-body">
        <h3 class="room-title">${r.roomTypeName} • ${r.floor}. Kat</h3>
        <div class="room-feats">
          <span class="room-feat">👥 ${r.capacity} Kişi</span>
          <span class="room-feat">🛁 Lüks Banyo</span>
          <span class="room-feat">📺 Smart TV</span>
          <span class="room-feat">🌊 Manzara</span>
        </div>
      </div>
      <div class="room-footer">
        <div class="room-price-col">
          <div class="room-price-big">${fmt(r.pricePerNight)}</div>
          <div class="room-price-lbl">₺ / gece</div>
          ${priceHtml}
        </div>
        <button class="btn-sec-card" ${isOccupied ? 'style="background:var(--red); border-color:var(--red)"' : ''} onclick="event.stopPropagation(); if('${r.status}' === 'Occupied') { showToast('Bu oda şu an dolu, içeride müşteri var!', 'error'); } else { openFModal(${r.id}); }">
          ${isOccupied ? 'Dolu (Müşteri Var)' : 'Seç & Rezervasyon →'}
        </button>
      </div>
    `;
    grid.appendChild(card);
  });
}

function showLoading(show) {
  getEl('rooms-loading').classList.toggle('hidden', !show);
  if (show) {
    getEl('rooms-grid').innerHTML = '';
    getEl('no-rooms').classList.add('hidden');
  }
}

/* ── FULL SCREEN MODAL ── */
function openFModal(id) {
  const room = allRooms.find(r => r.id === id);
  if (!room) return;
  selectedRoom = room;

  // Set Left Side Info
  getEl('fmodalImg').src = getRoomImage(room.roomTypeName);
  getEl('fmodalBadge').textContent = `✦ ODA ${room.roomNumber}`;
  getEl('fmodalRoomName').textContent = `${room.roomTypeName} (${room.floor}. Kat)`;
  getEl('fmodalPrice').textContent = fmt(room.pricePerNight);
  
  getEl('fmodalFeatures').innerHTML = `
    <span class="room-feat">👥 ${room.capacity} Yetişkin</span>
    <span class="room-feat">🛏️ Premium Yatak</span>
    <span class="room-feat">🚿 Yağmur Duş</span>
    <span class="room-feat">☕ Nespresso Makinesi</span>
    <span class="room-feat">🌐 Yüksek Hız WiFi</span>
    ${room.description ? `<span class="room-feat">ℹ️ ${room.description}</span>` : ''}
  `;

  // Init Form Right Side
  if (!checkIn) {
    const today = new Date();
    const tmrrw = new Date(today); tmrrw.setDate(today.getDate() + 1);
    const next  = new Date(today); next.setDate(today.getDate() + 3);
    const formD = d => d.toISOString().split('T')[0];
    
    getEl('rCheckIn').value = formD(tmrrw);
    getEl('rCheckOut').value = formD(next);
  } else {
    getEl('rCheckIn').value = checkIn;
    getEl('rCheckOut').value = checkOut;
  }
  
  getEl('rCheckIn').min = new Date().toISOString().split('T')[0];
  
  getEl('rAdults').value = Math.min(2, room.capacity);
  ['rName','rEmail','rPhone','rTc','rNotes'].forEach(i => getEl(i).value = '');
  
  updateModalPrice();

  // Show Modal
  getEl('fmodalFormWrap').classList.remove('hidden');
  getEl('fmodalSuccess').classList.add('hidden');
  getEl('fullModal').classList.remove('hidden');
  document.body.style.overflow = 'hidden';
}

function closeFModal() {
  getEl('fullModal').classList.add('hidden');
  document.body.style.overflow = '';
}

function updateModalPrice() {
  if (!selectedRoom) return;
  const ci = getEl('rCheckIn').value;
  const co = getEl('rCheckOut').value;
  
  if (!ci || !co) return;
  
  const d1 = new Date(ci), d2 = new Date(co);
  if (d2 > d1) {
    const n = Math.round((d2 - d1) / 86400000);
    const total = selectedRoom.pricePerNight * n;
    
    const html = `
      <div class="ps-row"><span>Konaklama (${n} Gece)</span><span>${fmt(total)} ₺</span></div>
      <div class="ps-row"><span>KDV & Konaklama Vergisi</span><span>Dahil</span></div>
      <div class="ps-row ps-total"><span>Garantili Toplam</span><span>${fmt(total)} ₺</span></div>
    `;
    getEl('priceSummary').innerHTML = html;
    if (getEl('priceSummary2')) getEl('priceSummary2').innerHTML = html;
    
    const minCo = new Date(d1); minCo.setDate(d1.getDate() + 1);
    getEl('rCheckOut').min = minCo.toISOString().split('T')[0];
  } else {
    const err = `<div class="ps-row" style="color:var(--red)">Hatalı tarih seçimi</div>`;
    getEl('priceSummary').innerHTML = err;
    if (getEl('priceSummary2')) getEl('priceSummary2').innerHTML = err;
  }
}

async function downloadPreviewPdf() {
  if (!selectedRoom) return showToast('Lütfen önce bir oda seçin', 'error');
  
  const fullName = getEl('rName').value.trim();
  const email = getEl('rEmail').value.trim();
  const checkInDate = getEl('rCheckIn').value;
  const checkOutDate = getEl('rCheckOut').value;

  if (!fullName) return showToast('PDF için Ad Soyad gereklidir', 'error');
  if (!email || !email.includes('@')) return showToast('PDF için Geçerli e-posta gereklidir', 'error');
  
  showToast('Önizleme PDF hazırlanıyor...', 'info');

  const payload = {
    roomId: selectedRoom.id,
    checkInDate: checkInDate,
    checkOutDate: checkOutDate,
    fullName: fullName,
    email: email,
    phone: getEl('rPhone').value.trim() || null,
    tcNo: getEl('rTc').value.trim() || null,
    adults: parseInt(getEl('rAdults').value),
    children: parseInt(getEl('rChildren').value),
    notes: getEl('rNotes').value.trim() || null
  };

  try {
    const res = await fetch(`${API}/api/reservations/online/preview-pdf`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.error || 'Hata');

    // PDF'i yeni sekmede aç / indir
    window.open(API + data.pdfUrl, '_blank');
    showToast('PDF Başarıyla oluşturuldu', 'success');
  } catch (e) {
    showToast('PDF oluşturulamadı: ' + e.message, 'error');
  }
}

function redirectToPayment() {
  if (!selectedRoom) return;

  // Validasyon
  const fullName = getEl('rName').value.trim();
  const email = getEl('rEmail').value.trim();
  const checkInDate = getEl('rCheckIn').value;
  const checkOutDate = getEl('rCheckOut').value;

  if (!fullName) return showToast('Lütfen ad soyad giriniz', 'error');
  if (!email || !email.includes('@')) return showToast('Geçerli e-posta giriniz', 'error');
  
  const d1 = new Date(checkInDate), d2 = new Date(checkOutDate);
  if (d2 <= d1) return showToast('Çıkış tarihi girişten sonra olmalıdır', 'error');

  // Rezervasyon verilerini hazırla
  const pendingReservation = {
    roomId: selectedRoom.id,
    checkInDate: checkInDate,
    checkOutDate: checkOutDate,
    fullName: fullName,
    email: email,
    phone: getEl('rPhone').value.trim() || null,
    tcNo: getEl('rTc').value.trim() || null,
    nationality: getEl('rNat').value,
    adults: parseInt(getEl('rAdults').value),
    children: parseInt(getEl('rChildren').value),
    notes: getEl('rNotes').value.trim() || null
  };

  // SessionStorage'a kaydet (Ödeme sayfasında kullanmak için)
  sessionStorage.setItem('pendingReservation', JSON.stringify(pendingReservation));
  sessionStorage.setItem('pendingRoom', JSON.stringify(selectedRoom));

  // Ödeme sayfasına yönlendir
  window.location.href = 'payment.html';
}

async function submitWithoutPayment(e) {
  if (!selectedRoom) return;
  if (e) e.preventDefault();

  // Validasyon
  const fullName = getEl('rName').value.trim();
  const email = getEl('rEmail').value.trim();
  const checkInDate = getEl('rCheckIn').value;
  const checkOutDate = getEl('rCheckOut').value;

  if (!fullName) return showToast('Lütfen ad soyad giriniz', 'error');
  if (!email || !email.includes('@')) return showToast('Geçerli e-posta giriniz', 'error');
  
  const d1 = new Date(checkInDate), d2 = new Date(checkOutDate);
  if (d2 <= d1) return showToast('Çıkış tarihi girişten sonra olmalıdır', 'error');

  const payload = {
    roomId: selectedRoom.id,
    checkInDate: checkInDate,
    checkOutDate: checkOutDate,
    fullName: fullName,
    email: email,
    phone: getEl('rPhone').value.trim() || null,
    tcNo: getEl('rTc').value.trim() || null,
    nationality: getEl('rNat').value,
    adults: parseInt(getEl('rAdults').value),
    children: parseInt(getEl('rChildren').value),
    notes: getEl('rNotes').value.trim() || null,
    isPaid: false,
    paymentMethod: 'Otelde Odeme'
  };

  const btn = event.currentTarget;
  const originalHtml = btn.innerHTML;
  btn.disabled = true;
  btn.innerHTML = '<div class="spinner"></div>';

  try {
    const res = await fetch(`${API}/api/reservations/online`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });
    
    const data = await res.json();
    if (!res.ok) throw new Error(data.error || 'Hata');

    // Başarı Ekranını Göster
    getEl('successMsg').textContent = 'Rezervasyonunuz Başarıyla Alındı!';
    getEl('successDetail').innerHTML = `
      <p><strong>Misafir:</strong> ${payload.fullName}</p>
      <p><strong>Oda:</strong> ${selectedRoom.roomTypeName} (Oda ${selectedRoom.roomNumber})</p>
      <p><strong>Tarih:</strong> ${new Date(checkInDate).toLocaleDateString('tr-TR')} - ${new Date(checkOutDate).toLocaleDateString('tr-TR')}</p>
      <p style="color:var(--gold); font-weight:700; margin-top:10px">🏨 Ödeme Varışta Otelde Yapılacaktır.</p>
    `;

    getEl('fmodalFormWrap').classList.add('hidden');
    getEl('fmodalSuccess').classList.remove('hidden');

  } catch (e) {
    showToast(e.message || 'Rezervasyon yapılamadı', 'error');
  } finally {
    btn.disabled = false;
    btn.innerHTML = originalHtml;
  }
}

/* ── UTILS ── */
let toastTimer;
function showToast(msg, type = '') {
  const t = getEl('toast');
  t.textContent = msg;
  t.className = `toast ${type}`;
  t.classList.remove('hidden');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => t.classList.add('hidden'), 4000);
}

// Escape tuşu modalı kapatsın
document.addEventListener('keydown', e => {
  if (e.key === 'Escape') { closeFModal(); closePortal(); }
});

/* ── GUEST PORTAL (LOGIN & HISTORY) ── */
let currentGuestEmail = localStorage.getItem('guestEmail') || null;

function checkPortalSession() {
  if (currentGuestEmail) {
    // Session var, ama modalı kullanıcı açınca veriyi çekeceğiz
  }
}

function openPortal() {
  getEl('portalOverlay').classList.remove('hidden');
  document.body.style.overflow = 'hidden';
  
  if (currentGuestEmail) showPortalDashboard();
  else showPortalLogin();
}

function closePortal() {
  getEl('portalOverlay').classList.add('hidden');
  document.body.style.overflow = '';
}

function showPortalLogin() {
  getEl('portalLogin').classList.remove('hidden');
  getEl('portalDashboard').classList.add('hidden');
}

let currentGuestCode = localStorage.getItem('guestCode') || null;

async function showPortalDashboard() {
  getEl('portalLogin').classList.add('hidden');
  getEl('portalDashboard').classList.remove('hidden');
  
  try {
    let url = `${API}/api/reservations/online/search?email=${encodeURIComponent(currentGuestEmail)}`;
    if (currentGuestCode) url += `&code=${encodeURIComponent(currentGuestCode)}`;
    
    const res = await fetch(url);
    const data = await res.json();
    
    if (!res.ok) throw new Error(data.error);
    
    // UI Update
    getEl('pWelcome').textContent = `Hoş Geldiniz, ${data[0]?.fullName || 'Misafir'}`;
    getEl('pTotalRes').textContent = data.length;
    getEl('pActiveStatus').textContent = translateStatus(data[0]?.status);
    
    const list = getEl('pList');
    list.innerHTML = '';
    data.forEach(r => {
      const item = document.createElement('div');
      item.className = 'tracking-item';
      item.style.marginBottom = '10px';
      
      let sClass = r.status.toLowerCase();
      if (sClass.includes('onay') || sClass.includes('giris')) sClass = 'approved';
      else if (sClass.includes('red')) sClass = 'rejected';
      else sClass = 'pending';

      item.innerHTML = `
        <div class="q-item-header">
          <strong>Talep #${r.id} ${r.resCode ? `<span style="color:var(--gold); font-size: 0.9em; margin-left: 5px;">(${r.resCode})</span>` : ''}</strong>
          <span class="q-status ${sClass}">${translateStatus(r.status)}</span>
        </div>
        <div class="q-item-body">
          <div>🛏️ <span>${r.roomTypeName} (Oda ${r.roomNumber})</span></div>
          <div>📅 <span>${new Date(r.checkInDate).toLocaleDateString('tr-TR')} - ${new Date(r.checkOutDate).toLocaleDateString('tr-TR')}</span></div>
        </div>
        ${r.rejectReason ? `<div class="q-reason"><strong>Red Nedeni:</strong> ${r.rejectReason}</div>` : ''}
      `;
      list.appendChild(item);
    });
  } catch (e) {
    showToast('Veriler alınamadı.', 'error');
    portalActionLogout();
  }
}

async function portalActionLogin() {
  const email = getEl('pEmail').value.trim();
  const code = getEl('pCode').value.trim();
  if (!email || !email.includes('@')) return showToast('Geçerli e-posta giriniz', 'error');
  
  currentGuestEmail = email;
  currentGuestCode = code;
  localStorage.setItem('guestEmail', email);
  if (code) localStorage.setItem('guestCode', code);
  else localStorage.removeItem('guestCode');
  
  showPortalDashboard();
}

function portalActionLogout() {
  currentGuestEmail = null;
  localStorage.removeItem('guestEmail');
  showPortalLogin();
}

function translateStatus(s) {
  if (!s) return '--';
  if (s === 'Bekliyor') return '⏳ Bekliyor';
  if (s === 'Onaylandi' || s === 'GirisYapildi') return '✅ Onaylandı';
  if (s === 'Reddedildi') return '❌ Reddedildi';
  return s;
}

/* ── PAYMENT PORTAL (ADMIN) ── */
function openPaymentPortal() {
  getEl('paymentPortalOverlay').classList.remove('hidden');
  document.body.style.overflow = 'hidden';
  
  // Set default dates (last 30 days)
  const end = new Date();
  const start = new Date(); start.setDate(end.getDate() - 30);
  const fmt = d => d.toISOString().split('T')[0];
  
  if (!getEl('payStart').value) getEl('payStart').value = fmt(start);
  if (!getEl('payEnd').value) getEl('payEnd').value = fmt(end);
  
  loadPayments();
}

function closePaymentPortal() {
  getEl('paymentPortalOverlay').classList.add('hidden');
  document.body.style.overflow = '';
}

async function loadPayments() {
  const search = getEl('paySearch').value;
  const method = getEl('payMethod').value;
  const start  = getEl('payStart').value;
  const end    = getEl('payEnd').value;
  
  const list = getEl('payList');
  list.innerHTML = '<tr><td colspan="8" class="center" style="padding:40px"><div class="gold-loader"></div></td></tr>';
  
  try {
    let url = `${API}/api/payments?startDate=${start}&endDate=${end}`;
    if (search) url += `&search=${encodeURIComponent(search)}`;
    if (method !== 'Tüm Yöntemler') url += `&method=${encodeURIComponent(method)}`;
    
    const res = await fetch(url);
    const data = await res.json();
    
    list.innerHTML = '';
    if (data.length === 0) {
      list.innerHTML = '<tr><td colspan="8" class="center" style="padding:40px; color:var(--muted)">Kayıt bulunamadı.</td></tr>';
    }
    
    data.forEach(p => {
      const tr = document.createElement('tr');
      tr.className = 'fade-in-up';
      tr.innerHTML = `
        <td class="td-id">#${p.id}</td>
        <td class="td-guest">${p.guestName}</td>
        <td><span class="td-room">${p.roomNumber}</span></td>
        <td class="td-id">#${p.reservationId}</td>
        <td class="td-amt">${fmt(p.amount)} ₺</td>
        <td class="td-method">${p.paymentMethodDisplay}</td>
        <td class="td-date">${new Date(p.paymentDate).toLocaleString('tr-TR')}</td>
        <td style="font-size:0.75rem; color:var(--muted); max-width:200px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;" title="${p.notes || ''}">${p.notes || '-'}</td>
      `;
      list.appendChild(tr);
    });

    // Load Stats
    loadPaymentStats();

  } catch (e) {
    list.innerHTML = '<tr><td colspan="8" class="center" style="padding:40px; color:var(--red)">Veriler yüklenemedi.</td></tr>';
  }
}

async function loadPaymentStats() {
  try {
    const res = await fetch(`${API}/api/payments/stats`);
    const stats = await res.json();
    
    const footer = getEl('payStats');
    footer.innerHTML = '';
    
    let totalAll = 0;
    stats.forEach(s => {
      totalAll += s.total;
      const box = document.createElement('div');
      box.className = 'p-stat-box';
      box.innerHTML = `
        <label>${s.method}</label>
        <strong>${fmt(s.total)} ₺</strong>
        <span>${s.count} İşlem</span>
      `;
      footer.appendChild(box);
    });

    // Toplam box
    if (stats.length > 0) {
        const totalBox = document.createElement('div');
        totalBox.className = 'p-stat-box';
        totalBox.style.borderLeft = '4px solid var(--gold)';
        totalBox.innerHTML = `
            <label>TOPLAM GELİR</label>
            <strong style="color:var(--white); font-size:1.4rem;">${fmt(totalAll)} ₺</strong>
            <span style="color:var(--gold)">Genel Toplam</span>
        `;
        footer.prepend(totalBox);
    }
  } catch (e) {}
}

async function trackReservationMain() {
  const email = getEl('tEmail').value.trim();
  if (!email || !email.includes('@')) return showToast('Lütfen geçerli e-posta giriniz', 'error');
  
  currentGuestEmail = email;
  localStorage.setItem('guestEmail', email);
  openPortal();
}
