const SENDERS = [
    'моя мама', 'мой папа', 'моя бабушка', 'мой дедушка', 
    'мой лучший друг', 'моя любимая', 'мой брат', 'моя сестра',
    'мой коллега', 'мой наставник', 'моя вторая половинка',
    'моя жена', 'мой муж', 'моя дочь', 'мой сын'
];

const MESSAGES = [
    'мой сладкий умничка..', 'ты мое солнышко! ❤️', 'очень тебя люблю!',
    'ты лучший человек!', 'с днем рождения, дорогой!', 'ты невероятный!',
    'спасибо, что ты есть!', 'ты мое вдохновение!', 'ты заслуживаешь это!',
    'ты особенный для меня!', 'я тобой горжусь!', 'ты делаешь мир лучше!',
    'ты мой герой!', 'без тебя я не я!', 'ты чудо!', 'обожаю тебя!'
];

function getRandomItem(arr) {
    return arr[Math.floor(Math.random() * arr.length)];
}

window.showGiftView = function(type, sender, message, emoji) {
    const oldView = document.getElementById('gift-view-overlay');
    if (oldView) oldView.remove();
    
    const overlay = document.createElement('div');
    overlay.id = 'gift-view-overlay';
    overlay.style.cssText = `
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, 0.8);
        display: flex;
        justify-content: center;
        align-items: center;
        z-index: 999999;
        animation: fadeIn 0.3s ease;
    `;
    
    const modal = document.createElement('div');
    modal.style.cssText = `
        background: linear-gradient(145deg, #2a2a2e, #1a1a1e);
        border-radius: 32px;
        padding: 40px 48px 36px;
        max-width: 420px;
        width: 90%;
        text-align: center;
        box-shadow: 0 32px 80px rgba(0, 0, 0, 0.9);
        border: 1px solid rgba(255, 255, 255, 0.08);
        animation: modalAppear 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
        position: relative;
        overflow: hidden;
    `;
    
    const typeNames = {
        '.gift': '🎁 Подарок',
        '.star': '⭐ Звёзды Telegram',
        '.premium': '⚡ Telegram Premium'
    };
    
    modal.innerHTML = `
        <div style="position: relative; z-index: 1;">
            <div style="
                width: 140px;
                height: 140px;
                margin: 0 auto 20px;
                display: flex;
                justify-content: center;
                align-items: center;
                background: radial-gradient(circle, rgba(255,255,255,0.05) 0%, transparent 70%);
                border-radius: 50%;
                border: 2px solid rgba(255, 255, 255, 0.06);
            ">
                <span style="font-size: 90px; filter: drop-shadow(0 8px 32px rgba(0,0,0,0.4));">
                    ${emoji}
                </span>
            </div>
            <h2 style="
                color: #ffffff;
                font-size: 22px;
                font-weight: 700;
                margin: 0 0 8px 0;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            ">
                ${typeNames[type] || 'Подарок'}
            </h2>
            <p style="
                color: #64b5f6;
                font-size: 18px;
                font-weight: 600;
                margin: 0 0 12px 0;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            ">
                От: ${sender}
            </p>
            <p style="
                color: #adacb0;
                font-size: 16px;
                font-style: italic;
                margin: 0 0 28px 0;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                line-height: 1.5;
                padding: 0 8px;
            ">
                "${message}"
            </p>
            <button onclick="document.getElementById('gift-view-overlay').remove()" style="
                background: linear-gradient(135deg, #2483e2, #1a6bc4);
                color: white;
                border: none;
                border-radius: 16px;
                padding: 14px 48px;
                font-size: 16px;
                font-weight: 600;
                cursor: pointer;
                transition: all 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
                box-shadow: 0 4px 24px rgba(36, 131, 226, 0.3);
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            " onmouseover="this.style.transform='scale(1.05)'" onmouseout="this.style.transform='scale(1)'">
                ✨ Отлично!
            </button>
        </div>
    `;
    
    overlay.appendChild(modal);
    document.body.appendChild(overlay);
    
    overlay.addEventListener('click', function(e) {
        if (e.target === this) this.remove();
    });
};

const GIFTS = {
    '.gift': {
        emoji: '🎁',
        title: 'Gift from ',
        btnText: 'View',
        btnBg: 'linear-gradient(135deg, #2483e2, #1a6bc4)'
    },
    '.star': {
        emoji: '⭐',
        title: 'Telegram Stars Received',
        btnText: 'Collect',
        btnBg: 'linear-gradient(135deg, #f5b041, #f39c12)'
    },
    '.premium': {
        emoji: '⚡',
        title: 'Telegram Premium Gift',
        btnText: 'Open',
        btnBg: 'linear-gradient(135deg, #8a2be2, #4a00e0)'
    }
};

const style = document.createElement('style');
style.innerHTML = `
    @keyframes shimmer {
        0% { background-position: -200% center; }
        100% { background-position: 200% center; }
    }
    @keyframes giftAppear {
        0% { opacity: 0; transform: scale(0.7) translateY(30px) rotate(-3deg); }
        100% { opacity: 1; transform: scale(1) translateY(0) rotate(0deg); }
    }
    @keyframes fadeIn {
        0% { opacity: 0; }
        100% { opacity: 1; }
    }
    @keyframes modalAppear {
        0% { opacity: 0; transform: scale(0.7) translateY(40px) rotate(-2deg); }
        100% { opacity: 1; transform: scale(1) translateY(0) rotate(0deg); }
    }
    
    .gift-wrapper {
        display: flex;
        justify-content: center;
        align-items: center;
        width: 100%;
        padding: 8px 0;
    }
    
    .interactive-gift {
        background: linear-gradient(145deg, #2a2a2e, #1a1a1e);
        border: 1px solid rgba(255, 255, 255, 0.08);
        border-radius: 32px;
        padding: 28px 24px 24px;
        width: 300px;
        max-width: 90%;
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;
        position: relative;
        overflow: hidden;
        box-shadow: 0 20px 60px rgba(0, 0, 0, 0.6), inset 0 1px 0 rgba(255, 255, 255, 0.05);
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
        margin: 0 auto;
        animation: giftAppear 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
        transition: all 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);
    }
    .interactive-gift:hover {
        transform: translateY(-4px) scale(1.02);
        box-shadow: 0 28px 80px rgba(0, 0, 0, 0.7);
        border-color: rgba(255, 255, 255, 0.12);
    }
    .gift-emoji-container {
        width: 120px;
        height: 120px;
        display: flex;
        justify-content: center;
        align-items: center;
        position: relative;
        z-index: 2;
        margin-bottom: 18px;
        background: radial-gradient(circle, rgba(255,255,255,0.05) 0%, transparent 70%);
        border-radius: 50%;
        border: 2px solid rgba(255, 255, 255, 0.06);
        transition: all 0.3s ease;
    }
    .interactive-gift:hover .gift-emoji-container {
        border-color: rgba(255, 255, 255, 0.15);
    }
    .gift-emoji {
        font-size: 74px;
        display: inline-block;
        filter: drop-shadow(0 8px 32px rgba(0,0,0,0.4));
        line-height: 1;
    }
    .gift-title {
        margin: 0 0 8px 0;
        font-size: 17px;
        font-weight: 600;
        color: #ffffff;
        letter-spacing: -0.2px;
        position: relative;
        z-index: 2;
        line-height: 1.4;
    }
    .gift-title a {
        color: #64b5f6;
        text-decoration: none;
        font-weight: 700;
        transition: all 0.2s ease;
        cursor: pointer;
    }
    .gift-title a:hover {
        color: #90caf9;
        text-decoration: underline;
    }
    .gift-message {
        font-size: 14px;
        color: #adacb0;
        font-style: italic;
        word-wrap: break-word;
        line-height: 1.5;
        position: relative;
        z-index: 2;
        margin-bottom: 20px;
        padding: 0 4px;
        min-height: 24px;
    }
    .gift-button {
        position: relative;
        z-index: 2;
        color: #ffffff;
        border: none;
        border-radius: 14px;
        width: 100%;
        padding: 14px 0;
        font-size: 15px;
        font-weight: 600;
        cursor: pointer;
        box-shadow: 0 4px 20px rgba(36, 131, 226, 0.35);
        transition: all 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
        letter-spacing: 0.3px;
        user-select: none;
        overflow: hidden;
    }
    .gift-button::before {
        content: '';
        position: absolute;
        inset: 0;
        background: linear-gradient(90deg, transparent, rgba(255,255,255,0.15), transparent);
        background-size: 200% 100%;
        animation: shimmer 3s infinite;
        pointer-events: none;
    }
    .gift-button:hover {
        transform: scale(1.04);
        box-shadow: 0 8px 32px rgba(36, 131, 226, 0.5);
    }
    .gift-button:active {
        transform: scale(0.94);
    }
    .sparkles-container {
        position: absolute;
        inset: 0;
        pointer-events: none;
        z-index: 1;
        overflow: hidden;
    }
    .sparkle {
        position: absolute;
        font-size: 14px;
        opacity: 0.4;
        animation: sparkleTwinkle 3s infinite;
    }
    @keyframes sparkleTwinkle {
        0%, 100% { opacity: 0.2; transform: scale(0.5); }
        50% { opacity: 0.8; transform: scale(1.2); }
    }
    .gift-shimmer {
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        height: 2px;
        background: linear-gradient(90deg, transparent, rgba(255,255,255,0.15), transparent);
        background-size: 200% 100%;
        animation: shimmer 3s infinite;
    }
`;
document.head.appendChild(style);

function replaceGiftsInDOM() {
    let bubbles = document.querySelectorAll('.bubble-content');
    
    bubbles.forEach(bubble => {
        if (bubble.dataset.giftRendered) return;

        let text = bubble.textContent.trim();
        
        for (let command in GIFTS) {
            if (text.startsWith(command)) {
                bubble.dataset.giftRendered = "true";
                
                const sender = getRandomItem(SENDERS);
                const message = getRandomItem(MESSAGES);
                
                let bubbleWrapper = bubble.closest('.bubble-content-wrapper');
                let mainBubble = bubble.closest('.bubble');
                
                if (mainBubble) {
                    mainBubble.style.cssText = `
                        background: none !important;
                        box-shadow: none !important;
                        max-width: 100% !important;
                        width: 100% !important;
                        display: flex !important;
                        justify-content: center !important;
                        padding: 0 !important;
                        margin: 0 !important;
                    `;
                }
                if (bubbleWrapper) {
                    bubbleWrapper.style.cssText = `
                        background: none !important;
                        box-shadow: none !important;
                        width: 100% !important;
                        display: flex !important;
                        justify-content: center !important;
                        padding: 0 !important;
                        margin: 0 !important;
                    `;
                }
                
                bubble.style.cssText = `
                    background: none !important;
                    padding: 0 !important;
                    margin: 0 !important;
                    width: 100% !important;
                    display: flex !important;
                    justify-content: center !important;
                `;

                let config = GIFTS[command];
                
                const sparkles = [];
                const positions = [
                    {top: '3%', left: '8%'}, {top: '8%', left: '88%'},
                    {top: '20%', left: '3%'}, {top: '25%', left: '92%'},
                    {top: '45%', left: '5%'}, {top: '50%', left: '85%'},
                    {top: '65%', left: '10%'}, {top: '70%', left: '82%'},
                    {top: '82%', left: '6%'}, {top: '88%', left: '88%'},
                    {top: '12%', left: '42%'}, {top: '78%', left: '45%'}
                ];
                
                positions.forEach((pos) => {
                    const delay = (Math.random() * 3.5).toFixed(1);
                    const size = (10 + Math.random() * 12).toFixed(0);
                    const colors = ['#ffe066', '#ffb3c1', '#00f5ff', '#ffffff', '#ff6b6b', '#a29bfe', '#ff9ff3'];
                    const color = colors[Math.floor(Math.random() * colors.length)];
                    sparkles.push(`
                        <div class="sparkle" style="
                            top: ${pos.top}; 
                            left: ${pos.left}; 
                            animation-delay: ${delay}s;
                            font-size: ${size}px;
                            color: ${color};
                        ">✦</div>
                    `);
                });

                const wrapper = document.createElement('div');
                wrapper.className = 'gift-wrapper';
                
                wrapper.innerHTML = `
                    <div class="interactive-gift" data-gift-type="${command}">
                        <div class="gift-shimmer"></div>
                        <div class="sparkles-container">
                            ${sparkles.join('')}
                        </div>
                        <div class="gift-emoji-container">
                            <span class="gift-emoji">${config.emoji}</span>
                        </div>
                        <div class="gift-title">
                            ${config.title}<a href="#" onclick="event.preventDefault();">${sender}</a>
                        </div>
                        <div class="gift-message">
                            "${message}"
                        </div>
                        <button class="gift-button" style="background: ${config.btnBg};" 
                                onclick="showGiftView('${command}', '${sender}', '${message}', '${config.emoji}')">
                            ${config.btnText}
                        </button>
                    </div>
                `;
                
                bubble.innerHTML = '';
                bubble.appendChild(wrapper);
            }
        }
    });
}

const observer = new MutationObserver((mutations) => {
    setTimeout(replaceGiftsInDOM, 50);
});

if (document.body) {
    observer.observe(document.body, { childList: true, subtree: true });
    replaceGiftsInDOM();
} else {
    document.addEventListener('DOMContentLoaded', () => {
        observer.observe(document.body, { childList: true, subtree: true });
        replaceGiftsInDOM();
    });
}
