import { useActionState, useEffect, useRef, useState } from "react";
import { useFormStatus } from "react-dom";
import {
  ApiError,
  createLink,
  deleteLink,
  getLinkStats,
  listLinks,
  updateLinkStatus,
} from "./api";
import type { Link } from "./api";
import { validateForm } from "./validation";
import type { FormErrors } from "./validation";
import "./App.css";

type Notice = { tone: "success" | "error"; message: string };
type CreateFormState = { errors: FormErrors };

function App() {
  const [links, setLinks] = useState<Link[]>([]);
  const [loading, setLoading] = useState(true);
  const [notice, setNotice] = useState<Notice | null>(null);
  const [platformEnabled, setPlatformEnabled] = useState(false);
  const [advancedOpen, setAdvancedOpen] = useState(false);
  const [busyCode, setBusyCode] = useState<string | null>(null);
  const [confirmCode, setConfirmCode] = useState<string | null>(null);
  const formRef = useRef<HTMLFormElement>(null);

  useEffect(() => {
    void listLinks()
      .then(setLinks)
      .catch((error: unknown) => showError(error))
      .finally(() => setLoading(false));
  }, []);

  function showError(error: unknown) {
    setNotice({
      tone: "error",
      message: error instanceof Error ? error.message : "Something went wrong.",
    });
  }

  async function createLinkAction(
    _previousState: CreateFormState,
    formData: FormData,
  ): Promise<CreateFormState> {
    const originalUrl = getFormValue(formData, "originalUrl");
    const customAlias = getFormValue(formData, "customAlias");
    const iosUrl = getFormValue(formData, "iosUrl");
    const androidUrl = getFormValue(formData, "androidUrl");
    const platformOverridesEnabled = formData.get("platformEnabled") === "on";

    const errors = validateForm(
      originalUrl,
      customAlias,
      platformOverridesEnabled,
      iosUrl,
      androidUrl,
    );
    if (Object.keys(errors).length > 0) {
      return { errors };
    }

    const overrides = platformOverridesEnabled
      ? {
          ...(iosUrl ? { ios: iosUrl } : {}),
          ...(androidUrl ? { android: androidUrl } : {}),
        }
      : undefined;
    const input = {
      originalUrl,
      ...(customAlias ? { customAlias } : {}),
      ...(overrides && Object.keys(overrides).length > 0
        ? { platformOverrides: overrides }
        : {}),
    };

    setNotice(null);
    try {
      const created = await createLink(input);
      setLinks((current) => [created, ...current]);
      formRef.current?.reset();
      setPlatformEnabled(false);
      setAdvancedOpen(false);
      setNotice({
        tone: "success",
        message: `${created.shortCode} is ready to share.`,
      });
      return { errors: {} };
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        return { errors: { customAlias: error.message } };
      }
      showError(error);
      return { errors: {} };
    }
  }

  const [formState, formAction] = useActionState(createLinkAction, {
    errors: {},
  });
  const errors = formState.errors;

  async function handleCopy(link: Link) {
    try {
      await navigator.clipboard.writeText(link.shortUrl);
      setNotice({ tone: "success", message: "Short URL copied to clipboard." });
    } catch {
      setNotice({
        tone: "error",
        message: "Clipboard access was unavailable. Copy the URL manually.",
      });
    }
  }

  async function handleRefresh(link: Link) {
    setBusyCode(link.shortCode);
    try {
      const refreshed = await getLinkStats(link.shortCode);
      setLinks((current) =>
        current.map((item) =>
          item.shortCode === link.shortCode ? refreshed : item,
        ),
      );
      setNotice({
        tone: "success",
        message: `Statistics refreshed for ${link.shortCode}.`,
      });
    } catch (error) {
      showError(error);
    } finally {
      setBusyCode(null);
    }
  }

  async function handleToggle(link: Link) {
    setBusyCode(link.shortCode);
    try {
      await updateLinkStatus(link.shortCode, !link.isActive);
      setLinks((current) =>
        current.map((item) =>
          item.shortCode === link.shortCode
            ? { ...item, isActive: !item.isActive }
            : item,
        ),
      );
      setNotice({
        tone: "success",
        message: `${link.shortCode} is now ${link.isActive ? "disabled" : "active"}.`,
      });
    } catch (error) {
      showError(error);
    } finally {
      setBusyCode(null);
    }
  }

  async function handleDelete() {
    if (!confirmCode) return;
    const code = confirmCode;
    setBusyCode(code);
    try {
      await deleteLink(code);
      setLinks((current) => current.filter((link) => link.shortCode !== code));
      setConfirmCode(null);
      setNotice({ tone: "success", message: `${code} was deleted.` });
    } catch (error) {
      showError(error);
    } finally {
      setBusyCode(null);
    }
  }

  return (
    <main className="shell">
      <header className="topbar">
        <div>
          <h1>URL Link Shortener</h1>
        </div>
      </header>

      <section className="create-panel" aria-labelledby="create-heading">
        <div className="section-heading">
          <div>
            <h2 id="create-heading">Create a short link</h2>
          </div>
        </div>
        <form action={formAction} ref={formRef} noValidate>
          <div className="form-grid">
            <label className="field field-wide">
              <span>
                Default destination <b>*</b>
              </span>
              <input
                name="originalUrl"
                placeholder="https://your-site.com/landing"
                aria-invalid={Boolean(errors.originalUrl)}
              />
              {errors.originalUrl && (
                <small className="field-error">{errors.originalUrl}</small>
              )}
            </label>
            <div className="field-control">
              <label className="toggle-label">
                <input
                  name="platformEnabled"
                  type="checkbox"
                  checked={platformEnabled}
                  onChange={(event) => setPlatformEnabled(event.target.checked)}
                />
                <span className="toggle" aria-hidden="true" />
                <span>Platform destinations</span>
              </label>
              <small>Route iOS and Android visitors to different URLs.</small>
            </div>
          </div>

          {platformEnabled && (
            <div className="platform-fields">
              <label className="field">
                <span>iOS destination</span>
                <input
                  name="iosUrl"
                  placeholder="https://download.your-site.com/app.ipa"
                  aria-invalid={Boolean(errors.ios)}
                />
                {errors.ios && (
                  <small className="field-error">{errors.ios}</small>
                )}
              </label>
              <label className="field">
                <span>Android destination</span>
                <input
                  name="androidUrl"
                  placeholder="https://download.your-site.com/app.apk"
                  aria-invalid={Boolean(errors.android)}
                />
                {errors.android && (
                  <small className="field-error">{errors.android}</small>
                )}
              </label>
              <p className="form-hint">
                Both are optional. Empty platforms use the default destination.
              </p>
            </div>
          )}

          <div className="form-footer">
            <button
              type="button"
              className="text-button"
              onClick={() => setAdvancedOpen((open) => !open)}
              aria-expanded={advancedOpen}
            >
              Advanced options{" "}
              <span aria-hidden="true">{advancedOpen ? "−" : "+"}</span>
            </button>
            <CreateSubmitButton />
          </div>
          {advancedOpen && (
            <div className="advanced-fields">
              <label className="field">
                <span>
                  Custom alias <em>optional</em>
                </span>
                <input
                  name="customAlias"
                  placeholder="Letters, numbers, hyphens, and underscores only."
                  aria-invalid={Boolean(errors.customAlias)}
                />
                {errors.customAlias && (
                  <small className="field-error">{errors.customAlias}</small>
                )}
              </label>
            </div>
          )}
        </form>
      </section>

      {notice && (
        <div className={`notice ${notice.tone}`} role="status">
          {notice.message}
          <button
            onClick={() => setNotice(null)}
            aria-label="Dismiss notification"
          >
            ×
          </button>
        </div>
      )}

      <section className="links-section" aria-labelledby="links-heading">
        <div className="list-heading">
          <div>
            <p className="eyebrow">YOUR WORKSPACE</p>
            <h2 id="links-heading">
              All links <span>{links.length}</span>
            </h2>
          </div>
        </div>
        {loading ? (
          <div className="empty-state">
            <div className="loader" />
            <p>Loading your links...</p>
          </div>
        ) : links.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">+</div>
            <h3>Your workspace is clear.</h3>
            <p>Create your first short link above to get started.</p>
          </div>
        ) : (
          <div className="link-list">
            {links.map((link) => (
              <LinkCard
                key={link.id}
                link={link}
                busy={busyCode === link.shortCode}
                onCopy={handleCopy}
                onRefresh={handleRefresh}
                onToggle={handleToggle}
                onDelete={() => setConfirmCode(link.shortCode)}
              />
            ))}
          </div>
        )}
      </section>

      {confirmCode && (
        <div className="modal-backdrop" role="presentation">
          <div
            className="modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="delete-title"
          >
            <p className="eyebrow">DELETE LINK</p>
            <h2 id="delete-title">Remove {confirmCode}?</h2>
            <p>
              This is a soft delete. The link will stop appearing here and
              cannot be redirected.
            </p>
            <div className="modal-actions">
              <button
                className="text-button"
                onClick={() => setConfirmCode(null)}
              >
                Cancel
              </button>
              <button
                className="danger-button"
                onClick={() => void handleDelete()}
                disabled={busyCode === confirmCode}
              >
                {busyCode === confirmCode ? "Deleting..." : "Delete link"}
              </button>
            </div>
          </div>
        </div>
      )}
    </main>
  );
}

function CreateSubmitButton() {
  const { pending } = useFormStatus();

  return (
    <button className="primary-button" disabled={pending}>
      {pending ? "Creating..." : "Create short link"}{" "}
      <span aria-hidden="true">→</span>
    </button>
  );
}

function getFormValue(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === "string" ? value.trim() : "";
}

function LinkCard({
  link,
  busy,
  onCopy,
  onRefresh,
  onToggle,
  onDelete,
}: {
  link: Link;
  busy: boolean;
  onCopy: (link: Link) => void;
  onRefresh: (link: Link) => void;
  onToggle: (link: Link) => void;
  onDelete: () => void;
}) {
  return (
    <article className={`link-card ${link.isActive ? "" : "is-disabled"}`}>
      <div className="card-main">
        <div className="link-identity">
          <span
            className={`status-dot ${link.isActive ? "active" : "disabled"}`}
          />
          <div>
            <a className="short-url" href={link.shortUrl}>
              {link.shortUrl.replace(/^https?:\/\//, "")}
            </a>
          </div>
        </div>
        <span
          className={`status-pill ${link.isActive ? "active" : "disabled"}`}
        >
          {link.isActive ? "Active" : "Disabled"}
        </span>
      </div>
      <div className="destination">
        <span className="meta-label">DEFAULT</span>
        <span title={link.originalUrl}>{link.originalUrl}</span>
      </div>
      {(link.platformOverrides.iosUrl || link.platformOverrides.androidUrl) && (
        <div className="override-row">
          <span className="meta-label">PLATFORM</span>
          {link.platformOverrides.iosUrl && (
            <span>
              <b>iOS</b> {link.platformOverrides.iosUrl}
            </span>
          )}
          {link.platformOverrides.androidUrl && (
            <span>
              <b>Android</b> {link.platformOverrides.androidUrl}
            </span>
          )}
        </div>
      )}
      <div className="card-footer">
        <div className="stats">
          <span>
            <strong>{link.clickCount}</strong> clicks
          </span>
          <span>Created {formatDate(link.createdAtUtc)}</span>
          <span>
            Last access{" "}
            {link.lastAccessedAtUtc
              ? formatDate(link.lastAccessedAtUtc)
              : "Never"}
          </span>
        </div>
        <div className="card-actions">
          <button
            onClick={() => onCopy(link)}
            aria-label={`Copy ${link.shortCode}`}
          >
            Copy
          </button>
          <button
            onClick={() => onRefresh(link)}
            disabled={busy}
            aria-label={`Refresh statistics for ${link.shortCode}`}
          >
            {busy ? "..." : "↻"}
          </button>
          <button onClick={() => onToggle(link)} disabled={busy}>
            {link.isActive ? "Disable" : "Enable"}
          </button>
          <button className="delete-action" onClick={onDelete} disabled={busy}>
            Delete
          </button>
        </div>
      </div>
    </article>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export default App;
