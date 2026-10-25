import { Heart } from "lucide-react";

import { Button } from "@/components/ui/button";

export default function App() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-6 bg-background px-6 text-center text-foreground">
      <div className="flex size-16 items-center justify-center rounded-full bg-primary/10">
        <Heart className="size-8 text-primary" aria-hidden="true" />
      </div>
      <div className="space-y-2">
        <h1 className="text-4xl font-semibold tracking-tight">Memoly</h1>
        <p className="text-balance text-muted-foreground">
          Keep life's moments together.
        </p>
      </div>
      <Button>Get started</Button>
    </main>
  );
}
