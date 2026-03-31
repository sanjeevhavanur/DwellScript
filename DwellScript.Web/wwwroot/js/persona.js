// ── Persona Targeting Feature ─────────────────────────────────────────

var selectedPersona = null;
var personaGenerated = false;
var personaOutput = '';
var personaGenerating = false;

var PERSONAS = [
    {
        key: 'remote-worker', name: 'Remote Worker', icon: 'bi-laptop',
        emphasis: 'Home office, WiFi, quiet environment',
        iconBg: '#EFF6FF', iconColor: '#2563EB', iconBgDark: 'rgba(37,99,235,.12)'
    },
    {
        key: 'pet-owner', name: 'Pet Owner', icon: 'bi-heart-fill',
        emphasis: 'Backyard, pet policy, nearby parks',
        iconBg: '#FFF7ED', iconColor: '#EA580C', iconBgDark: 'rgba(249,115,22,.12)'
    },
    {
        key: 'commuter', name: 'Commuter', icon: 'bi-car-front-fill',
        emphasis: 'Highway access, parking, transit links',
        iconBg: '#F0FDF4', iconColor: '#16A34A', iconBgDark: 'rgba(22,163,74,.12)'
    },
    {
        key: 'outdoor-enthusiast', name: 'Outdoor Enthusiast', icon: 'bi-tree-fill',
        emphasis: 'Green space, trails, storage, outdoor areas',
        iconBg: '#F0FDF4', iconColor: '#15803D', iconBgDark: 'rgba(21,128,61,.12)'
    },
    {
        key: 'urban-lifestyle', name: 'Urban Lifestyle', icon: 'bi-buildings-fill',
        emphasis: 'Walkability, dining, shopping, nightlife',
        iconBg: '#F5F3FF', iconColor: '#6D28D9', iconBgDark: 'rgba(109,40,217,.12)'
    },
    {
        key: 'long-term-resident', name: 'Long-Term Resident', icon: 'bi-house-heart-fill',
        emphasis: 'Stability, lease flexibility, community feel',
        iconBg: '#FEF9C3', iconColor: '#CA8A04', iconBgDark: 'rgba(202,138,4,.12)'
    }
];

function initPersonaCards() {
    var isDark = document.documentElement.getAttribute('data-theme') === 'dark';
    var html = PERSONAS.map(function(p) {
        var bg = isDark ? p.iconBgDark : p.iconBg;
        return '<div class="persona-card" id="pcard-' + p.key + '" onclick="selectPersona(\'' + p.key + '\')">' +
            '<div class="persona-check"><i class="bi bi-check"></i></div>' +
            '<div class="persona-icon-box" style="background:' + bg + ';color:' + p.iconColor + '">' +
                '<i class="bi ' + p.icon + '"></i>' +
            '</div>' +
            '<div class="persona-name">' + p.name + '</div>' +
            '<div class="persona-emphasis">' + p.emphasis + '</div>' +
        '</div>';
    }).join('');
    $('#personaGrid').html(html);
}

function selectPersona(key) {
    selectedPersona = PERSONAS.find(function(p) { return p.key === key; }) || null;
    $('.persona-card').removeClass('selected');
    if (selectedPersona) {
        $('#pcard-' + key).addClass('selected');
        var icon = '<i class="bi ' + selectedPersona.icon + '" style="color:' + selectedPersona.iconColor + ';margin-right:6px"></i>';
        $('#personaStatus')
            .html(icon + 'Generating for: <strong>' + selectedPersona.name + '</strong>')
            .addClass('has-persona');
        $('#btnPersonaGenerate').prop('disabled', false);
    }
}

function generatePersonaListing() {
    if (!selectedPersona || personaGenerating) return;
    personaGenerating = true;

    var $btn = $('#btnPersonaGenerate');
    $btn.prop('disabled', true).html('<i class="bi bi-hourglass-split"></i>Generating\u2026');

    $('#personaPreGen').hide();
    $('#personaOutputCard').hide();
    $('#personaFhaStrip').hide();
    $('#personaRefineDrawer').removeClass('open');
    $('#personaSkeleton').show();

    $.ajax({
        url: '/api/generation/persona',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ propertyId: propId, personaKey: selectedPersona.key }),
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        success: function(res) {
            personaOutput = res.output;
            personaGenerated = true;
            renderPersonaOutput(res.output);
            $('#personaFhaStrip').show();
            loadPersonaHistory();
            toastr.success('Persona listing generated!', '', { timeOut: 3500 });
        },
        error: function(xhr) {
            $('#personaSkeleton').hide();
            $('#personaPreGen').show();
            toastr.error(xhr.responseJSON && xhr.responseJSON.message ? xhr.responseJSON.message : 'Generation failed.', 'Error');
        },
        complete: function() {
            personaGenerating = false;
            $btn.prop('disabled', false).html('<i class="bi bi-stars"></i>Generate Persona Listing');
        }
    });
}

function renderPersonaOutput(text) {
    if (!selectedPersona) return;
    var isDark = document.documentElement.getAttribute('data-theme') === 'dark';
    var bg = isDark ? selectedPersona.iconBgDark : selectedPersona.iconBg;

    $('#personaCardIcon')
        .attr('style', 'background:' + bg + ';color:' + selectedPersona.iconColor)
        .html('<i class="bi ' + selectedPersona.icon + '"></i>');
    $('#personaCardSubtitle').text(selectedPersona.name + ' \u00b7 LTR');
    $('#personaOutputBody').text(text);

    var wordCount = text.split(/\s+/).filter(Boolean).length;
    $('#personaWordCount').html('<i class="bi bi-text-paragraph"></i> ' + wordCount + ' words');

    $('#personaSkeleton').hide();
    $('#personaPreGen').hide();
    $('#personaRegenOverlay').removeClass('show');
    $('#personaOutputCard').show();
}

function regenPersona() {
    if (!selectedPersona || personaGenerating) return;
    personaGenerating = true;
    $('#personaRegenOverlay').addClass('show');

    $.ajax({
        url: '/api/generation/persona',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ propertyId: propId, personaKey: selectedPersona.key }),
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        success: function(res) {
            personaOutput = res.output;
            renderPersonaOutput(res.output);
            loadPersonaHistory();
            toastr.success('Persona listing regenerated.');
        },
        error: function(xhr) {
            $('#personaRegenOverlay').removeClass('show');
            toastr.error(xhr.responseJSON && xhr.responseJSON.message ? xhr.responseJSON.message : 'Regeneration failed.', 'Error');
        },
        complete: function() { personaGenerating = false; }
    });
}

function togglePersonaRefine() {
    $('#personaRefineDrawer').toggleClass('open');
    if ($('#personaRefineDrawer').hasClass('open')) {
        $('#personaRefineInput').focus();
    }
}

function submitPersonaRefine() {
    var instruction = $('#personaRefineInput').val().trim();
    if (!instruction) { toastr.warning('Please enter a refinement instruction.'); return; }
    if (!selectedPersona || personaGenerating) return;

    personaGenerating = true;
    $('#personaRefineInput').val('');
    $('#personaRefineDrawer').removeClass('open');
    $('#personaRegenOverlay').addClass('show');

    $.ajax({
        url: '/api/generation/persona-refine',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ propertyId: propId, personaKey: selectedPersona.key, instruction: instruction }),
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        success: function(res) {
            personaOutput = res.output;
            renderPersonaOutput(res.output);
            loadPersonaHistory();
            var preview = instruction.length > 40 ? instruction.substring(0, 40) + '\u2026' : instruction;
            toastr.success('\u201c' + preview + '\u201d applied.', 'Refinement Applied');
        },
        error: function(xhr) {
            $('#personaRegenOverlay').removeClass('show');
            toastr.error(xhr.responseJSON && xhr.responseJSON.message ? xhr.responseJSON.message : 'Refinement failed.', 'Error');
        },
        complete: function() { personaGenerating = false; }
    });
}

function copyPersonaOutput() {
    if (!personaGenerated || !personaOutput) { toastr.warning('Nothing to copy yet.'); return; }
    navigator.clipboard.writeText(personaOutput).then(function() { showCopied('Persona listing'); });
}

function downloadPersonaOutput() {
    if (!personaGenerated || !personaOutput) { toastr.warning('Nothing to download yet.'); return; }
    var filename = 'property-' + (typeof propId !== 'undefined' ? propId : 'listing') +
                   '-persona-' + (selectedPersona ? selectedPersona.key : 'listing') + '.txt';
    var blob = new Blob([personaOutput], { type: 'text/plain' });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url; a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

// ── Persona History ──────────────────────────────────────────────────

function loadPersonaHistory() {
    if (typeof propId === 'undefined') return;
    $.ajax({
        url: '/api/generation/persona-history/' + propId,
        method: 'GET',
        success: function(items) {
            $('#personaHistBadge').text(items.length);
            if (!items || items.length === 0) {
                $('#personaHistoryList').html(
                    '<p style="font-size:13px;color:var(--text-tertiary);margin:0">No persona listings yet. Generate one below to get started.</p>'
                );
                return;
            }
            var html = items.map(function(item) {
                var p = PERSONAS.find(function(x) { return x.key === item.personaKey; });
                var personaName = p ? p.name : (item.personaKey || 'Unknown');
                var iconClass = p ? p.icon : 'bi-person';
                var iconColor = p ? p.iconColor : '#6B7280';
                var typeLabel = item.type === 'PersonaRefine' ? ' &middot; Refined' : '';
                var date = new Date(item.createdAt);
                var dateStr = date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) +
                              ' ' + date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
                return '<div class="persona-hist-item" style="display:flex;align-items:center;gap:10px;padding:9px 12px;border-radius:8px;cursor:pointer;border:1px solid var(--border);margin-bottom:6px;background:var(--card-bg)">' +
                    '<i class="bi ' + iconClass + '" style="color:' + iconColor + ';font-size:15px;flex-shrink:0"></i>' +
                    '<div style="flex:1;min-width:0">' +
                        '<div style="font-size:13px;font-weight:600;color:var(--text-primary)">' + personaName + typeLabel + '</div>' +
                        '<div style="font-size:11.5px;color:var(--text-tertiary)">' + dateStr + ' &middot; ' + item.wordCount + ' words</div>' +
                    '</div>' +
                    '<button class="ds-icon-btn" title="Load" onclick="loadPersonaOne(' + item.id + ')" style="flex-shrink:0"><i class="bi bi-arrow-up-right-square"></i></button>' +
                    '<button class="ds-icon-btn" title="Delete" onclick="deletePersonaHistory(' + item.id + ',event)" style="flex-shrink:0;color:var(--danger,#dc2626)"><i class="bi bi-trash3"></i></button>' +
                '</div>';
            }).join('');
            $('#personaHistoryList').html(html);
        },
        error: function() {
            // silently fail — history is non-critical
        }
    });
}

function loadPersonaOne(id) {
    $.ajax({
        url: '/api/generation/persona/' + id,
        method: 'GET',
        success: function(res) {
            var p = PERSONAS.find(function(x) { return x.key === res.personaKey; });
            if (p) {
                selectedPersona = p;
                $('.persona-card').removeClass('selected');
                $('#pcard-' + p.key).addClass('selected');
                var icon = '<i class="bi ' + p.icon + '" style="color:' + p.iconColor + ';margin-right:6px"></i>';
                $('#personaStatus')
                    .html(icon + 'Showing: <strong>' + p.name + '</strong>')
                    .addClass('has-persona');
                $('#btnPersonaGenerate').prop('disabled', false);
            }
            personaOutput = res.output;
            personaGenerated = true;
            renderPersonaOutput(res.output);
            $('#personaFhaStrip').show();
            toastr.info('Loaded persona listing from history.');
        },
        error: function() {
            toastr.error('Could not load persona listing.');
        }
    });
}

function deletePersonaHistory(id, e) {
    e.stopPropagation();
    if (!confirm('Delete this persona listing from history?')) return;
    $.ajax({
        url: '/api/generation/persona/' + id,
        method: 'DELETE',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        success: function() {
            toastr.success('Persona listing deleted.');
            loadPersonaHistory();
        },
        error: function() {
            toastr.error('Could not delete persona listing.');
        }
    });
}

// Enter key in refine input
$(document).on('keypress', '#personaRefineInput', function(e) {
    if (e.which === 13) submitPersonaRefine();
});

// Re-render icon backgrounds when theme changes
$(document).on('click', '#themeToggle', function() {
    setTimeout(function() {
        if (selectedPersona && personaGenerated) {
            var isDark = document.documentElement.getAttribute('data-theme') === 'dark';
            var bg = isDark ? selectedPersona.iconBgDark : selectedPersona.iconBg;
            $('#personaCardIcon').css('background', bg);
        }
        // Re-render cards with correct icon bg colors
        if ($('#personaGrid').children().length) {
            var prevSelected = selectedPersona ? selectedPersona.key : null;
            initPersonaCards();
            if (prevSelected) selectPersona(prevSelected);
        }
    }, 50);
});
