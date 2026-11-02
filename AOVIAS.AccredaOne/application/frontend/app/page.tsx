import styles from "./page.module.css";

export default function Home() {
  return (
    <div className={styles.page}>
      <main className={styles.main}>
        <h1>AccredaOne</h1>
        <p className={styles.lead}>
          Accreditation knowledge discovery and explanation platform (scaffolding
          only).
        </p>
        <p className={styles.note}>
          DEV-03 reversible foundation. No product workflows, authentication, or
          accreditation source content. Phase 1 is not authorized.
        </p>
      </main>
    </div>
  );
}
