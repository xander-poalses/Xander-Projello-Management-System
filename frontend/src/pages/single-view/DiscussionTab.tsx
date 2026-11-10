import React, { useEffect, useState } from "react";
import "./single-project-view.css";
import "./DiscussionTab.css";
import { API_BASE_URL } from "../../config";
import { getToken } from "../../auth/tokenStore";
import { Skeleton, SkeletonGroup } from "../../components/Skeleton";
import EmptyState from "../../components/EmptyState";

type DiscussionReaction = {
  id: number;
  userId: string;
  userFullName: string;
  emoji: string;
  createdAt: string;
};

type DiscussionComment = {
  id: number;
  userId: string;
  userFullName: string;
  commentText: string;
  createdAt: string;
  replies?: DiscussionComment[]; // For potential nested comments in the future
};

type DiscussionPost = {
  id: number;
  projectId: number;
  userId: string;
  userFullName: string;
  caption: string | null;
  imageUrl: string;
  createdAt: string;
  reactions: DiscussionReaction[] | null;
  comments: DiscussionComment[] | null;
};

interface DiscussionTabProps {
  projectId: number;
}

const reactionOptions = ["👍", "❤️", "😂", "👏", "🔥"];

export default function DiscussionTab({ projectId }: DiscussionTabProps) {
  const [posts, setPosts] = useState<DiscussionPost[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [commentDrafts, setCommentDrafts] = useState<Record<number, string>>({});
  const [submittingId, setSubmittingId] = useState<number | null>(null);
  const [imagePreviewUrl, setImagePreviewUrl] = useState<string | null>(null);
  const [currentUserId, setCurrentUserId] = useState<string | null>(null);

  const [zoomedImageUrl, setZoomedImageUrl] = useState<string | null>(null);
  const [zoomLevel, setZoomLevel] = useState(1);

  const openImageZoom = (url: string) => {
    setZoomedImageUrl(url);
    setZoomLevel(1);
  };

  const closeImageZoom = () => {
    setZoomedImageUrl(null);
    setZoomLevel(1);
  };

  const fetchPosts = async () => {
    setLoading(true);
    setError(null);

    try {
      const token = getToken();
      const res = await fetch(`${API_BASE_URL}/api/projects/${projectId}/updates`, {
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
      });

      if (!res.ok) throw new Error("Failed to fetch discussion");

      const data: DiscussionPost[] = await res.json();
      console.log("GET /api/projects/:id/updates →", data.slice(0,5));
      setPosts(data);
    } catch (err: any) {
      setError(err.message || "Failed to load discussion");
    } finally {
      setLoading(false);
    }
  };

  // NEW: Refresh without showing full loading screen (prevents page "refresh" feeling)
  const refreshPosts = async () => {
    try {
      const token = getToken();
      const res = await fetch(`${API_BASE_URL}/api/projects/${projectId}/updates`, {
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
      });

      if (!res.ok) throw new Error("Failed to refresh");

      const data: DiscussionPost[] = await res.json();
      setPosts(data);
    } catch (err: any) {
      console.error("Refresh failed:", err);
    }
  };

  useEffect(() => {
    if (projectId) {
      fetchPosts();
    }
  }, [projectId]);

  useEffect(() => {
    const token = getToken();
    if (!token) return;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const id =
        payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] ||
        payload["nameid"] ||
        payload["sub"] ||
        payload["id"];
      setCurrentUserId(id ?? null);
    } catch { /* ignore */ }
  }, []);

  const handleReact = async (updateId: number, emoji: string) => {
    try {
      setSubmittingId(updateId);
      const token = getToken();

      const res = await fetch(`${API_BASE_URL}/api/projects/${projectId}/updates/${updateId}/react`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ emoji }),
      });

      if (!res.ok) throw new Error("Failed to save reaction");

      await refreshPosts(); //Changed to prevent full page refresh
    } catch (err: any) {
      setError(err.message || "Failed to add reaction");
    } finally {
      setSubmittingId(null);
    }
  };

  const handleCommentSubmit = async (e: React.FormEvent, updateId: number) => {
    e.preventDefault();
    const commentText = commentDrafts[updateId]?.trim();
    if (!commentText) return;

    try {
      setSubmittingId(updateId);
      const token = getToken();

      const res = await fetch(`${API_BASE_URL}/api/projects/${projectId}/updates/${updateId}/comments`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ commentText }),
      });

      if (!res.ok) throw new Error("Failed to save comment");

      setCommentDrafts((current) => ({ ...current, [updateId]: "" }));
      await refreshPosts(); // Changed to prevent full page refresh
    } catch (err: any) {
      setError(err.message || "Failed to add comment");
    } finally {
      setSubmittingId(null);
    }
  };

  // NEW: Delete entire post (image + caption + all comments)
  const handleDeletePost = async (updateId: number) => {
    if (!confirm("Delete this entire post and all its comments? This cannot be undone.")) return;

    try {
      setSubmittingId(updateId);
      const token = getToken();

      const res = await fetch(`${API_BASE_URL}/api/projects/${projectId}/updates/${updateId}`, {
        method: "DELETE",
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });

      if (!res.ok) throw new Error("Failed to delete post");

      await refreshPosts();
    } catch (err: any) {
      setError(err.message || "Failed to delete post");
    } finally {
      setSubmittingId(null);
    }
  };

  // NEW: Delete single comment
  const handleDeleteComment = async (updateId: number, commentId: number) => {
    if (!confirm("Delete this comment?")) return;

    try {
      setSubmittingId(commentId);
      const token = getToken();

      const res = await fetch(`${API_BASE_URL}/api/projects/${projectId}/updates/${updateId}/comments/${commentId}`, {
        method: "DELETE",
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });

      if (!res.ok) throw new Error("Failed to delete comment");

      await refreshPosts();
    } catch (err: any) {
      setError(err.message || "Failed to delete comment");
    } finally {
      setSubmittingId(null);
    }
  };

  const handleCreatePost = async (e: React.FormEvent) => {
    e.preventDefault();

    const caption = newPostCaption.trim();
    const imageFile = imageFileInput;

    if (!caption && !imageFile) return;

    try {
      setSubmittingId(0);
      const token = getToken();

      const formData = new FormData();
      formData.append("caption", caption);
      if (imageFile) {
        formData.append("image", imageFile);
      }

      const res = await fetch(`${API_BASE_URL}/api/projects/${projectId}/updates`, {
        method: "POST",
        headers: {
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: formData,
      });

      if (!res.ok) throw new Error("Failed to post update");

      setNewPostCaption("");
      setImageFileInput(null);
      setImagePreviewUrl(null);
      await refreshPosts();
    } catch (err: any) {
      setError(err.message || "Failed to post update");
    } finally {
      setSubmittingId(null);
    }
  };

  const [newPostCaption, setNewPostCaption] = useState("");
  const [imageFileInput, setImageFileInput] = useState<File | null>(null);

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    setImageFileInput(file);
    setImagePreviewUrl(file ? URL.createObjectURL(file) : null);
  };

  useEffect(() => {
    return () => {
      if (imagePreviewUrl) {
        URL.revokeObjectURL(imagePreviewUrl);
      }
    };
  }, [imagePreviewUrl]);

    if (loading) {
    return (
      <SkeletonGroup label="Loading discussion" className="discussion-tab">
        <Skeleton height={190} radius="var(--radius-card)" />
        <Skeleton height={260} radius="var(--radius-card)" />
      </SkeletonGroup>
    );
  }
  if (error) {
    return (
      <div className="discussion-tab">
        <EmptyState tone="error" title="Couldn't load the discussion" message="Check your connection and open the tab again." />
      </div>
    );
  }

  return (
        <div className="discussion-tab">
      <section className="discussion-tab__composer">
        <h2 className="pj-section-title">Discussion</h2>
        <p className="discussion-tab__subtitle">
          Share updates, react, and reply in the project thread.
        </p>

                <form onSubmit={handleCreatePost} className="discussion-tab__form">
          <textarea
            value={newPostCaption}
            onChange={(e) => setNewPostCaption(e.target.value)}
            placeholder="Write an update..."
                        rows={4}
            aria-label="Write an update"
            className="discussion-tab__field discussion-tab__textarea"
          />
          <div className="discussion-tab__form-row">
            <label htmlFor="discussion-image" className="discussion-tab__label">
              Attach an image
            </label>
            <input
              id="discussion-image"
              type="file"
              accept="image/*"
              onChange={handleImageChange}
            />
            <button type="submit" className="btn-primary" style={{ minWidth: 120 }}>
              Post update
            </button>
          </div>
        </form>
      </section>

            <section className="discussion-tab__post-list">
        {posts.length === 0 && (
          <EmptyState
            title="No updates yet"
            message="Post the first update above: progress, a photo from site, or a question for the team."
          />
        )}
        {posts.map(post => {
          //Safe access to reactions
          const safeReactions = post.reactions ?? [];
          const reactionCounts = safeReactions.reduce<Record<string, number>>((acc, r) => {
            acc[r.emoji] = (acc[r.emoji] || 0) + 1;
            return acc;
          }, {});

          const userReacted = (emoji: string) =>
            safeReactions.some(r => r.emoji === emoji && r.userId === currentUserId);

          return (
                        <article key={post.id} className="discussion-tab__post-card">
              {/* Post header: author + timestamp */}
                            <div className="discussion-tab__post-header">
                <div>
                  <div className="discussion-tab__author">{post.userFullName || "Unknown user"}</div>
                  <div className="discussion-tab__meta">{post.createdAt ? new Date(post.createdAt).toLocaleString() : ""}</div>
                </div>
              </div>

              {/* Image */}
              {post.imageUrl && (
                <div style={{ position: "relative" }}>
                  <img
                    src={post.imageUrl}
                    alt="Post"
                    onClick={() => openImageZoom(post.imageUrl)}
                    style={{ width: "100%", maxHeight: 360, objectFit: "cover", display: "block" }}
                  />
                  {/* Delete entire post button */}
                  <button
                    onClick={() => handleDeletePost(post.id)}
                    disabled={submittingId === post.id}
                    style={{
                      position: "absolute",
                      top: 12,
                      right: 12,
                      background: "rgb(var(--error-rgb) / 0.9)",
                      color: "var(--white)",
                      border: "none",
                                            borderRadius: "var(--radius-control)",
                      padding: "6px 12px",
                      fontSize: 13,
                      cursor: "pointer"
                    }}
                  >
                    Delete Post
                  </button>
                </div>
              )}

                            <div className="discussion-tab__post-content">
                {/* Caption */}
                {post.caption && (
                  <p className="discussion-tab__post-caption">
                    {post.caption}
                  </p>
                )}

                {/* Reactions */}
                <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 12 }}>
                  {reactionOptions.map((emoji) => (
                    <button
                      key={emoji}
                      type="button"
                      onClick={() => handleReact(post.id, emoji)}
                      className={`discussion-reaction-btn ${userReacted(emoji) ? "discussion-reaction-btn-reacted" : ""}`}
                      disabled={submittingId === post.id}
                    >
                      <span style={{ marginRight: 6 }}>{emoji}</span>
                      <span style={{ fontWeight: 600 }}>{reactionCounts[emoji] || 0}</span>
                    </button>
                  ))}
                </div>

                {/* Comments header */}
                                <div className="discussion-tab__comments-label">
                  Comments {post.comments?.length ? `(${post.comments.length})` : ""}
                </div>

                {/* Comments list */}
                <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                  {post.comments && post.comments.length > 0 ? (
                    post.comments.map((comment) => (
                                            <div key={comment.id} className="discussion-tab__comment">
                        <div className="discussion-tab__comment-header">
                          <strong>{comment.userFullName || "Unknown user"}</strong>
                          <span className="discussion-tab__meta">{comment.createdAt ? new Date(comment.createdAt).toLocaleString() : ""}</span>
                        </div>
                        <p style={{ margin: "8px 0 0", lineHeight: 1.5 }}>{comment.commentText}</p>
                        
                        {/* Delete comment button (own comments + Owner/Admin) */}
                        {comment.userId === currentUserId && (
                          <button
                            type="button"
                            onClick={() => handleDeleteComment(post.id, comment.id)}
                            disabled={submittingId === comment.id}
                            style={{ marginTop: 8, fontSize: 13, color: "var(--error-text)", background: "none", border: "none", cursor: "pointer" }}
                          >
                            Delete
                          </button>
                        )}
                      </div>
                    ))
                  ) : (
                                        <div className="discussion-tab__meta">No comments yet. Be the first to reply.</div>
                  )}
                </div>

                {/* Comment composer */}
                                <form onSubmit={(e) => handleCommentSubmit(e, post.id)} className="discussion-tab__comment-form">
                  <input
                    aria-label="Write a comment"
                    value={commentDrafts[post.id] || ""}
                    onChange={(e) => setCommentDrafts((current) => ({ ...current, [post.id]: e.target.value }))}
                                        placeholder="Write a comment..."
                    className="discussion-tab__field"
                  />
                  <button type="submit" className="btn-secondary" disabled={submittingId === post.id}>Reply</button>
                </form>
              </div>
            </article>
          );
        })}
      </section>

      {zoomedImageUrl && (
        <div
          onClick={() => setZoomedImageUrl(null)}
          style={{
            position: "fixed",
            inset: 0,
            background: "rgb(var(--shadow-rgb) / 0.8)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            zIndex: 1000,
            padding: 20,
          }}
        >
          <img
            src={zoomedImageUrl}
            alt="Zoomed post"
            onClick={(e) => e.stopPropagation()}
            style={{
              maxWidth: "95vw",
              maxHeight: "95vh",
              objectFit: "contain",
                            borderRadius: "var(--radius-card)",
              boxShadow: "0 20px 60px rgb(var(--shadow-rgb) / 0.4)",
            }}
          />
        </div>
      )}
    </div>
  );
}