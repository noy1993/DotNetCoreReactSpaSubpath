import { Link } from "react-router-dom";

export function Home() {
    return (
        <>
            <Link to="/about">About</Link>
            <div>hello home</div>
        </>
    )
}