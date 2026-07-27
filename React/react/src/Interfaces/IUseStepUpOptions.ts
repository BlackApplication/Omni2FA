/** Options accepted by <c>useStepUp</c>. */
export interface IUseStepUpOptions {
    /**
     * Register this hook's <c>confirmTwoFactor</c> on the client while the component is mounted, so the
     * library's own step-up-gated calls (remove method, regenerate recovery codes, enroll start) prompt and
     * retry on their own. Default <c>true</c>. Set <c>false</c> when the host registers its own handler via
     * <c>client.setStepUpHandler</c> — the last registration wins, and this one would overwrite it.
     */
    handleClientStepUp?: boolean;
}
