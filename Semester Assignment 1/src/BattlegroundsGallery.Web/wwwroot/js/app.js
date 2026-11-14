/* =========================================================
   随从图鉴 · 交互脚本
   目前包含：等级筛选 / 种族筛选 / 关键词搜索 / 空状态
   ========================================================= */

const cards = Array.from(document.querySelectorAll(".minion-card"));
const tierButtons = Array.from(document.querySelectorAll(".tier-chip"));
const tribeButtons = Array.from(document.querySelectorAll(".tribe-chip"));
const resetButtons = Array.from(document.querySelectorAll("[data-reset]"));
const searchInput = document.querySelector("#keyword-search");
const resultCount = document.querySelector("#result-count");
const emptyState = document.querySelector("#empty-state");
const clearFiltersButton = document.querySelector("#clear-filters");
const comingSoonButtons = Array.from(document.querySelectorAll("[data-coming-soon]"));
const toast = document.querySelector("#toast");

const filters = {
  tier: null,
  tribe: null,
  keyword: "",
};

let toastTimer;

/** 根据当前筛选条件显示卡牌，并更新计数与空状态。 */
function applyFilters() {
  const keyword = filters.keyword.trim().toLowerCase();
  let visibleCount = 0;

  cards.forEach((card) => {
    const matchesTier = !filters.tier || card.dataset.tier === filters.tier;
    const cardTribes = (card.dataset.tribes || card.dataset.tribe || "").split(",");
    const matchesTribe = !filters.tribe || cardTribes.includes(filters.tribe);
    const matchesKeyword = !keyword || card.textContent.toLowerCase().includes(keyword);
    const matches = matchesTier && matchesTribe && matchesKeyword;

    card.hidden = !matches;
    if (matches) visibleCount += 1;
  });

  resultCount.textContent = String(visibleCount);
  emptyState.hidden = visibleCount !== 0;

  // 点击“全选”时高亮，表示该行当前没有限制条件。
  resetButtons.forEach((button) => {
    const isTierReset = button.dataset.reset === "tier";
    const isAllSelected = isTierReset ? !filters.tier : !filters.tribe;
    button.classList.toggle("is-active", isAllSelected);
    button.setAttribute("aria-pressed", String(isAllSelected));
  });
}

/** 同步按钮高亮和 aria-pressed 状态。 */
function syncChoiceButtons(buttons, activeValue, dataKey) {
  buttons.forEach((button) => {
    const isActive = button.dataset[dataKey] === activeValue;
    button.classList.toggle("is-active", isActive);
    button.setAttribute("aria-pressed", String(isActive));
  });
}

tierButtons.forEach((button) => {
  button.setAttribute("aria-pressed", "false");

  button.addEventListener("click", () => {
    const selectedTier = button.dataset.tier;
    filters.tier = filters.tier === selectedTier ? null : selectedTier;
    syncChoiceButtons(tierButtons, filters.tier, "tier");
    applyFilters();
  });
});

tribeButtons.forEach((button) => {
  button.setAttribute("aria-pressed", "false");

  button.addEventListener("click", () => {
    const selectedTribe = button.dataset.tribe;
    filters.tribe = filters.tribe === selectedTribe ? null : selectedTribe;
    syncChoiceButtons(tribeButtons, filters.tribe, "tribe");
    applyFilters();
  });
});

resetButtons.forEach((button) => {
  button.addEventListener("click", () => {
    if (button.dataset.reset === "tier") {
      filters.tier = null;
      syncChoiceButtons(tierButtons, null, "tier");
    } else {
      filters.tribe = null;
      syncChoiceButtons(tribeButtons, null, "tribe");
    }

    applyFilters();
  });
});

searchInput.addEventListener("input", () => {
  filters.keyword = searchInput.value;
  applyFilters();
});

// 按 Esc 快速清空搜索词。
searchInput.addEventListener("keydown", (event) => {
  if (event.key === "Escape" && searchInput.value) {
    searchInput.value = "";
    filters.keyword = "";
    applyFilters();
  }
});

clearFiltersButton.addEventListener("click", () => {
  filters.tier = null;
  filters.tribe = null;
  filters.keyword = "";
  searchInput.value = "";
  syncChoiceButtons(tierButtons, null, "tier");
  syncChoiceButtons(tribeButtons, null, "tribe");
  applyFilters();
  searchInput.focus();
});

/** 底部短提示，给尚未实现的栏目一个友好的反馈。 */
function showToast(message) {
  window.clearTimeout(toastTimer);
  toast.textContent = message;
  toast.classList.add("is-visible");
  toastTimer = window.setTimeout(() => {
    toast.classList.remove("is-visible");
  }, 2200);
}

comingSoonButtons.forEach((button) => {
  button.addEventListener("click", () => {
    showToast(`「${button.textContent.trim()}」栏目还在准备中，下次继续做。`);
  });
});

/* =========================================================
   官图卡牌的 3D 悬停效果
   对应原网页的 perspective(1000px) + rotateX / rotateY
   ========================================================= */

const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)");

document.querySelectorAll(".official-card").forEach((card) => {
  const inner = card.querySelector(".official-card__inner");

  card.addEventListener("pointermove", (event) => {
    if (event.pointerType === "touch" || reduceMotion.matches) return;

    const rect = card.getBoundingClientRect();
    const offsetX = (event.clientX - rect.left) / rect.width - 0.5;
    const offsetY = (event.clientY - rect.top) / rect.height - 0.5;
    const rotateY = offsetX * 10;
    const rotateX = offsetY * -10;

    inner.style.transform = `perspective(1000px) rotateX(${rotateX.toFixed(2)}deg) rotateY(${rotateY.toFixed(2)}deg) scale3d(1.02, 1.02, 1.02)`;
  });

  card.addEventListener("pointerleave", () => {
    inner.style.transform = "";
  });
});

// 初始化计数和“全选”按钮状态。
applyFilters();
