// Your topics (docs/news-section-handoff.md, phase 5): the follow form's live preview. After a half-second pause in
// typing it asks /me/topics/preview once and shows the verdict, a 7-day bar chart and the latest matches with the
// terms marked. Everything is written as text, never HTML. Without JavaScript the form still posts; it just has no
// preview.
(function () {
    'use strict';
    const verdicts = {
        good: (n, perDay) => [
            `${n} ${n === 1 ? 'story' : 'stories'} in the past week · ${perDay}`,
            'A good fit: enough to be useful, not so many it floods your news.',
        ],
        broad: (n, perDay) => [
            `Very broad · ${n} this week`,
            `${perDay[0].toUpperCase()}${perDay.slice(1)}: it will flood “Since your last visit”. Try another word, an Except word, or one section. You can still follow it.`,
        ],
        none: () => [
            'No matches yet',
            'Nothing in the past week. That’s fine for a rare topic; you’ll see a count when one appears. Check the spelling.',
        ],
    };
    function perDayText(n) {
        const a = n / 7;
        return a < 1 ? 'fewer than one a day' : `about ${Math.round(a)} a day`;
    }
    function setup() {
        const form = document.querySelector('[data-topic-form]');
        const preview = document.querySelector('[data-topic-preview]');
        if (!form || !preview || form.dataset.topicReady) return;
        form.dataset.topicReady = '1';
        const terms = form.querySelector('[name=terms]'),
            except = form.querySelector('[name=except]'),
            hub = form.querySelector('[name=hub]'),
            submit = form.querySelector('[data-topic-submit]');
        const editing = /\/me\/topics\/\d+$/.test(form.action);
        let timer = 0,
            controller = null;
        function label() {
            if (editing) return;
            const first = terms.value.split(',')[0].trim();
            submit.textContent = first.length >= 2 ? `Follow “${first}”` : 'Follow';
        }
        async function update() {
            controller?.abort();
            if (terms.value.trim().length < 2) {
                preview.hidden = true;
                return;
            }
            controller = new AbortController();
            const query = new URLSearchParams({ terms: terms.value, except: except.value, hub: hub.value });
            let data;
            try {
                const response = await fetch('me/topics/preview?' + query, {
                    credentials: 'same-origin',
                    cache: 'no-store',
                    signal: controller.signal,
                    headers: { Accept: 'application/json' },
                });
                if (!response.ok) return;
                data = await response.json();
            } catch {
                return;
            }
            const verdict = preview.querySelector('[data-topic-verdict]');
            const [headline, detail] = verdicts[data.verdict](data.total, perDayText(data.total));
            verdict.className = 'tp-verdict tp-verdict--' + data.verdict;
            verdict.querySelector('b').textContent = headline;
            verdict.querySelector('span').textContent = detail;

            const chart = preview.querySelector('[data-topic-chart]');
            const most = Math.max(1, ...data.perDay.map((d) => d.count));
            chart.setAttribute(
                'aria-label',
                'Matches per day, past week: ' + data.perDay.map((d) => `${d.day} ${d.count}`).join(', '),
            );
            chart.replaceChildren(
                ...data.perDay.map((d, i) => {
                    const day = document.createElement('span');
                    day.className = 'tp-chart__day' + (i === data.perDay.length - 1 ? ' tp-chart__day--today' : '');
                    const bar = document.createElement('span');
                    bar.className = 'tp-chart__bar';
                    bar.style.height = `${Math.round((d.count / most) * 100)}%`;
                    const holder = document.createElement('span');
                    holder.className = 'tp-chart__track';
                    holder.append(bar);
                    const name = document.createElement('span');
                    name.className = 'tp-chart__name';
                    name.textContent = d.day;
                    day.append(holder, name);
                    return day;
                }),
            );

            const list = preview.querySelector('[data-topic-latest]');
            list.replaceChildren(
                ...data.latest.map((item) => {
                    const li = document.createElement('li');
                    const link = document.createElement(item.url ? 'a' : 'span');
                    if (item.url) {
                        link.href = item.url;
                        link.target = '_blank';
                        link.rel = 'noopener noreferrer';
                    }
                    const title = document.createElement('b');
                    for (const part of item.title) {
                        if (part.match) {
                            const mark = document.createElement('mark');
                            mark.textContent = part.text;
                            title.append(mark);
                        } else title.append(part.text);
                    }
                    const meta = document.createElement('small');
                    meta.textContent = item.meta;
                    link.append(title, meta);
                    li.append(link);
                    return li;
                }),
            );
            list.previousElementSibling.hidden = data.latest.length === 0;
            preview.querySelector('[data-topic-hidden]').textContent =
                data.hiddenByExcept > 0
                    ? `${data.hiddenByExcept} ${data.hiddenByExcept === 1 ? 'story' : 'stories'} hidden by Except: ${data.except.join(', ')}`
                    : '';
            preview.hidden = false;
        }
        function schedule() {
            label();
            clearTimeout(timer);
            timer = setTimeout(update, 500);
        }
        terms.addEventListener('input', schedule);
        except.addEventListener('input', schedule);
        hub.addEventListener('change', schedule);
        label();
        if (terms.value.trim()) update();
    }
    setup();
    // Enhanced navigation can replace the page without rerunning global scripts.
    if (window.Blazor?.addEventListener) window.Blazor.addEventListener('enhancedload', setup);
})();
