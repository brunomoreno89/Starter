// wwwroot/js/ui/modal.js

export function openModal(title, contentBuilder, onConfirm, options = {}) {
  const {
    confirmText = 'Save',
    cancelText  = 'Cancel',
    confirmClass = '',   // ex.: 'danger'
    closeOnConfirm = true
  } = options;

  const backdrop = document.createElement('div'); backdrop.className='modal-backdrop';
  const modal = document.createElement('div'); modal.className='modal';
  modal.innerHTML = `
    <header><h3>${title}</h3><button class="close-btn" title="Close">×</button></header>
    <div class="body"></div>
    <footer>
      <button class="secondary" data-action="cancel">${cancelText}</button>
      <button class="primary ${confirmClass}" data-action="confirm">${confirmText}</button>
    </footer>`;
  backdrop.appendChild(modal);
  document.body.appendChild(backdrop);

  const body = modal.querySelector('.body');
  const btnClose = modal.querySelector('.close-btn');
  const btnCancel = modal.querySelector('[data-action="cancel"]');
  const btnConfirm = modal.querySelector('[data-action="confirm"]');

  const cleanup = () => backdrop.remove();

  btnClose.onclick = cleanup;
  btnCancel.onclick = cleanup;

  btnConfirm.onclick = async () => {
    try {
      await onConfirm?.();
      if (closeOnConfirm) cleanup();
    } catch (e) {
      // mostre o erro dentro do modal
      let msg = modal.querySelector('.modal-error');
      if (!msg) {
        msg = document.createElement('div');
        msg.className = 'alert error modal-error';
        modal.querySelector('footer').insertAdjacentElement('beforebegin', msg);
      }
      msg.textContent = e.friendly || e.message || String(e);
    }
  };

  contentBuilder?.(body);

  return { close: cleanup, el: modal };
}

export function openConfirm(title, messageHtml, onConfirm, options = {}) {
  const opts = {
    confirmText: options.confirmText ?? 'Delete',
    confirmClass: options.confirmClass ?? 'danger',
    cancelText: options.cancelText ?? 'Cancel',
    closeOnConfirm: options.closeOnConfirm ?? true
  };
  return openModal(
    title,
    (body) => {
      body.innerHTML = `
        <div class="confirm">
          ${messageHtml}
        </div>`;
    },
    onConfirm,
    opts
  );
}
