/**
 * Client-seitiges DSGVO Consent-Management
 * Keine Daten werden an Instagram gesendet, bis der Nutzer zustimmt.
 */

(function () {
  const STORAGE_KEY = 'instagram_embed_consent';
  const CONSENT_EXPIRY_DAYS = 30;

  function hasConsent() {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) return false;
    try {
      const consent = JSON.parse(stored);
      const age = (Date.now() - consent.timestamp) / (1000 * 60 * 60 * 24);
      return age <= CONSENT_EXPIRY_DAYS && consent.granted === true;
    } catch { return false; }
  }

  function grantConsent() {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ granted: true, timestamp: Date.now() }));
  }

  function revokeConsent() {
    localStorage.removeItem(STORAGE_KEY);
  }

  function showConsentBanner(container) {
    const banner = document.createElement('div');
    banner.className = 'instagram-consent-banner';
    banner.innerHTML = `
      <div class="consent-content">
        <div class="consent-icon">📸</div>
        <p class="consent-text">${window.InstagramEmbed.consentText}</p>
        <div class="consent-actions">
          <button class="consent-accept" id="insta-consent-accept">Akzeptieren</button>
          <button class="consent-decline" id="insta-consent-decline">Ablehnen</button>
        </div>
        <a href="${window.InstagramEmbed.privacyPolicyUrl}" class="consent-privacy-link">Datenschutzerklärung</a>
      </div>`;
    container.innerHTML = '';
    container.appendChild(banner);
    document.getElementById('insta-consent-accept').addEventListener('click', () => { grantConsent(); loadPost(container); });
    document.getElementById('insta-consent-decline').addEventListener('click', () => {
      container.innerHTML = '<p class="consent-declined">Instagram-Beitrag wird nicht angezeigt.</p>';
    });
  }

  async function loadPost(container) {
    container.innerHTML = '<div class="instagram-loading">Lade neuesten Beitrag…</div>';
    try {
      const response = await fetch('/api/latest-post');
      if (!response.ok) throw new Error('Server-Fehler');
      const post = await response.json();

      const wrapper = document.createElement('div');
      wrapper.className = 'instagram-post-wrapper';

      const link = document.createElement('a');
      link.href = post.permalink;
      link.target = '_blank';
      link.rel = 'noopener noreferrer nofollow';
      link.setAttribute('aria-label', `Instagram-Beitrag von @${post.username} öffnen`);

      const img = document.createElement('img');
      img.src = post.image;
      img.alt = post.caption;
      img.className = 'instagram-post-image';
      img.loading = 'lazy';

      const caption = document.createElement('p');
      caption.className = 'instagram-post-caption';
      const maxLen = 120;
      caption.textContent = post.caption.length > maxLen ? post.caption.substring(0, maxLen) + '…' : post.caption;

      const meta = document.createElement('div');
      meta.className = 'instagram-post-meta';
      meta.innerHTML = `<span class="instagram-username">@${post.username}</span><span class="instagram-badge">Instagram</span>`;

      link.appendChild(img);
      wrapper.appendChild(link);
      wrapper.appendChild(caption);
      wrapper.appendChild(meta);
      container.innerHTML = '';
      container.appendChild(wrapper);
    } catch (error) {
      container.innerHTML = '<p class="instagram-error">Beitrag konnte nicht geladen werden.</p>';
      console.error('[Instagram-Embed]', error);
    }
  }

  function init() {
    const container = document.getElementById('instagram-embed');
    if (!container) { console.warn('[Instagram-Embed] Container nicht gefunden'); return; }
    if (window.InstagramEmbed.consentMode === 'always' || hasConsent()) { loadPost(container); }
    else { showConsentBanner(container); }
  }

  if (document.readyState === 'loading') { document.addEventListener('DOMContentLoaded', init); }
  else { init(); }

  window.InstagramEmbed = window.InstagramEmbed || {};
  window.InstagramEmbed.revokeConsent = () => { revokeConsent(); location.reload(); };
  window.InstagramEmbed.reload = () => {
    const container = document.getElementById('instagram-embed');
    if (container && hasConsent()) loadPost(container);
  };
})();